using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PacmanDominio.DTO.TiempoReal;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazLN;
using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Salas;
using PacmanLogicaNegocios.Juego.Sistemas;
using PacmanUtilitarios;

namespace PacmanLogicaNegocios.Juego.Salas;

public sealed class GestorSalas : IGestorSalas, IAsyncDisposable, IDisposable
{
    private const int CantidadMaximaSalas = 5;
    private const string CaracteresCodigo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly TimeSpan IntervaloReintento = TimeSpan.FromSeconds(1);

    // Este candado solo protege los índices. Nunca espera una compuerta ni una operación externa.
    private readonly object _indices = new();
    private readonly Dictionary<string, ContextoSala> _salas = new(StringComparer.Ordinal);
    private readonly Dictionary<int, ContextoSala> _usuarios = new();
    private readonly Dictionary<string, ContextoSala> _conexiones = new(StringComparer.Ordinal);
    private readonly HashSet<ContextoSala> _ciclos = new();
    private readonly CancellationTokenSource _cierre = new();
    private readonly CancellationToken _cancelacion;
    private readonly IServiceScopeFactory _ambitos;
    private readonly INotificadorJuego _notificador;
    private readonly ILogger<GestorSalas> _logger;
    private readonly TimeProvider _tiempo;
    private readonly SistemaNiveles _niveles = new();
    private readonly AsignadorPersonajes _personajes = new();
    private bool _cerrando;
    private Task? _disposicion;

    public GestorSalas(
        IServiceScopeFactory ambitos,
        INotificadorJuego notificador,
        ILogger<GestorSalas> logger,
        TimeProvider? tiempo = null)
    {
        ArgumentNullException.ThrowIfNull(ambitos);
        ArgumentNullException.ThrowIfNull(notificador);
        ArgumentNullException.ThrowIfNull(logger);

        _ambitos = ambitos;
        _notificador = notificador;
        _logger = logger;
        _tiempo = tiempo ?? TimeProvider.System;
        _cancelacion = _cierre.Token;
    }

    public async Task<Respuesta<SalaDto>> CrearAsync(
        int idUsuario, string nombreUsuario, string idConexion)
    {
        try
        {
            ValidarIngreso(idUsuario, nombreUsuario, idConexion);
            ContextoSala contexto;

            lock (_indices)
            {
                VerificarDisponible();

                if (_salas.Count >= CantidadMaximaSalas)
                {
                    return Respuesta<SalaDto>.Fail("Ya hay cinco salas; espere a que se libere una.");
                }

                VerificarIdentidadesLibres(idUsuario, idConexion);
                string codigo = CrearCodigo();
                var sala = new Sala(codigo);
                sala.Ingresar(idUsuario, nombreUsuario, idConexion);
                contexto = new ContextoSala(sala, _tiempo);
                _salas.Add(codigo, contexto);
                _usuarios.Add(idUsuario, contexto);
                _conexiones.Add(idConexion, contexto);
                _ciclos.Add(contexto);
                contexto.Ciclo = Task.Run(() => EjecutarCicloAsync(contexto));
            }

            return await EjecutarEnSalaAsync(contexto, actual =>
                Task.FromResult(Respuesta<SalaDto>.Ok(SalaDto.Crear(actual.Sala))), notificar: true);
        }
        catch (Exception excepcion)
        {
            return Fallo<SalaDto>(excepcion);
        }
    }

    public async Task<Respuesta<IEnumerable<SalaDto>>> ListarAsync()
    {
        try
        {
            ContextoSala[] contextos;

            lock (_indices)
            {
                VerificarDisponible();
                contextos = _salas.Values.ToArray();
            }

            var consultas = contextos.Select(contexto => EjecutarEnSalaAsync(contexto, actual =>
                Task.FromResult(Respuesta<SalaDto>.Ok(SalaDto.Crear(actual.Sala)))));
            var respuestas = await Task.WhenAll(consultas);

            lock (_indices)
            {
                VerificarDisponible();
            }

            var salas = respuestas.Where(respuesta => respuesta.Success)
                .Select(respuesta => respuesta.Data!).ToList().AsReadOnly();

            return Respuesta<IEnumerable<SalaDto>>.Ok(salas);
        }
        catch (Exception excepcion)
        {
            return Fallo<IEnumerable<SalaDto>>(excepcion);
        }
    }

    public Task<Respuesta<SalaDto>> ObtenerAsync(string codigoSala)
    {
        return EjecutarEnSalaAsync(codigoSala, contexto =>
            Task.FromResult(Respuesta<SalaDto>.Ok(SalaDto.Crear(contexto.Sala))));
    }

    public async Task<Respuesta<SalaDto>> ObtenerSalaDeUsuarioAsync(int idUsuario)
    {
        try
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
            ContextoSala contexto;

            lock (_indices)
            {
                VerificarDisponible();

                if (!_usuarios.TryGetValue(idUsuario, out contexto!))
                {
                    return Respuesta<SalaDto>.Fail("El usuario no tiene una plaza en ninguna sala.");
                }
            }

            return await EjecutarEnSalaAsync(contexto, actual =>
                Task.FromResult(Respuesta<SalaDto>.Ok(SalaDto.Crear(actual.Sala))));
        }
        catch (Exception excepcion)
        {
            return Fallo<SalaDto>(excepcion);
        }
    }

    public Task<Respuesta<SalaDto>> IngresarAsync(
        string codigoSala, int idUsuario, string nombreUsuario, string idConexion)
    {
        return EjecutarEnSalaAsync(codigoSala, contexto =>
        {
            ValidarIngreso(idUsuario, nombreUsuario, idConexion);
            VerificarSinInicioPendiente(contexto);

            lock (_indices)
            {
                VerificarDisponible();
                VerificarIdentidadesLibres(idUsuario, idConexion);

                if (!contexto.Sala.Ingresar(idUsuario, nombreUsuario, idConexion))
                {
                    return Task.FromResult(Respuesta<SalaDto>.Fail("La sala no admite el ingreso."));
                }

                _usuarios.Add(idUsuario, contexto);
                _conexiones.Add(idConexion, contexto);
            }

            contexto.SalaPendiente = true;
            return Task.FromResult(Respuesta<SalaDto>.Ok(SalaDto.Crear(contexto.Sala)));
        }, notificar: true);
    }

    public Task<Respuesta<SalaDto>> SalirAsync(string codigoSala, int idUsuario)
    {
        return EjecutarEnSalaAsync(codigoSala, contexto =>
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
            var jugador = contexto.Sala.Jugadores.FirstOrDefault(actual => actual.IdUsuario == idUsuario);
            bool serieEnCurso = contexto.Sala.Estado is EstadoSala.Jugando or EstadoSala.Transicion;

            if (jugador == null || !contexto.Sala.Salir(idUsuario))
            {
                return Task.FromResult(Respuesta<SalaDto>.Fail("El usuario no pertenece a la sala."));
            }

            contexto.InicioPendiente?.SalaPreparada.Salir(idUsuario);

            lock (_indices)
            {
                _usuarios.Remove(idUsuario);
                _conexiones.Remove(jugador.IdConexion);
            }

            if (contexto.Sala.Vacia && (serieEnCurso || contexto.InicioPendiente != null) &&
                !SerieCompleta(contexto))
            {
                contexto.Abandonada = true;
            }

            MarcarCambioDeSala(contexto);
            return Task.FromResult(Respuesta<SalaDto>.Ok(SalaDto.Crear(contexto.Sala)));
        }, notificar: true);
    }

    public async Task<Respuesta<SalaDto>> DesconectarAsync(string idConexion)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);
            ContextoSala contexto;

            lock (_indices)
            {
                VerificarDisponible();

                if (!_conexiones.TryGetValue(idConexion, out contexto!))
                {
                    return Respuesta<SalaDto>.Fail("La conexión ya no está registrada.");
                }
            }

            return await EjecutarEnSalaAsync(contexto, actual =>
            {
                if (!actual.Sala.Desconectar(idConexion))
                {
                    return Task.FromResult(Respuesta<SalaDto>.Fail("La conexión ya no está vigente."));
                }

                actual.InicioPendiente?.SalaPreparada.Desconectar(idConexion);
                MarcarCambioDeSala(actual);
                return Task.FromResult(Respuesta<SalaDto>.Ok(SalaDto.Crear(actual.Sala)));
            }, notificar: true);
        }
        catch (Exception excepcion)
        {
            return Fallo<SalaDto>(excepcion);
        }
    }

    public Task<Respuesta<SalaDto>> ReconectarAsync(
        string codigoSala, int idUsuario, string idConexion)
    {
        return EjecutarEnSalaAsync(codigoSala, async contexto =>
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
            ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);
            var jugador = contexto.Sala.Jugadores.FirstOrDefault(actual => actual.IdUsuario == idUsuario);

            if (jugador == null)
            {
                return Respuesta<SalaDto>.Fail("El usuario no tiene una plaza reservada en esta sala.");
            }

            string anterior;

            lock (_indices)
            {
                VerificarDisponible();

                if (_conexiones.ContainsKey(idConexion) && jugador.IdConexion != idConexion)
                {
                    return Respuesta<SalaDto>.Fail("La conexión ya pertenece a otro jugador.");
                }

                 anterior = jugador.IdConexion;

                if (!contexto.Sala.Reconectar(idUsuario, idConexion))
                {
                    return Respuesta<SalaDto>.Fail("La sala no permite la reconexión.");
                }

                contexto.InicioPendiente?.SalaPreparada.Reconectar(idUsuario, idConexion);
                _conexiones.Remove(anterior);
                _conexiones[idConexion] = contexto;
            }

            if (anterior != idConexion)
            {
                await _notificador.NotificarSesionReemplazadaAsync(anterior, contexto.Sala.Codigo);
            }

            MarcarCambioDeSala(contexto);
            await EnviarInstantaneasAsync(contexto, idConexion);
            return Respuesta<SalaDto>.Ok(SalaDto.Crear(contexto.Sala));
        }, notificar: true);
    }

    public Task<Respuesta<SalaDto>> ConfigurarAsync(
        string codigoSala, int idUsuario, ConfiguracionSalaDto configuracion)
    {
        return EjecutarEnSalaAsync(codigoSala, async contexto =>
        {
            ArgumentNullException.ThrowIfNull(configuracion);
            VerificarAnfitrionEnLobby(contexto, idUsuario);
            var opciones = configuracion.ConvertirAConfiguracion();

            await using var ambito = _ambitos.CreateAsyncScope();
            var servicio = ambito.ServiceProvider.GetRequiredService<IDificultadLN>();
            var respuesta = await servicio.ObtenerAsync(new TDificultad { IdDificultad = opciones.IdDificultad });
            var dificultad = ExigirDatos(respuesta, "No se pudo consultar la dificultad");

            if (dificultad.IdDificultad != opciones.IdDificultad)
            {
                throw new InvalidOperationException("La dificultad devuelta no corresponde a la solicitada.");
            }

            if (!contexto.Sala.Configurar(idUsuario, opciones))
            {
                return Respuesta<SalaDto>.Fail("Solo el anfitrión puede configurar una sala en el lobby.");
            }

            contexto.SalaPendiente = true;
            return Respuesta<SalaDto>.Ok(SalaDto.Crear(contexto.Sala));
        }, notificar: true);
    }

    public Task<Respuesta<SalaDto>> ElegirRolAsync(string codigoSala, int idUsuario, string? rol)
    {
        return EjecutarEnSalaAsync(codigoSala, contexto =>
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
            VerificarSinInicioPendiente(contexto);
            var bando = CatalogoPersonajes.DesdeCodigo(rol);

            if (rol != null && bando == null)
            {
                throw new ArgumentException("El rol debe ser PACMAN, FANTASMA o null para sortearlo.");
            }

            if (!contexto.Sala.ElegirRol(idUsuario, bando))
            {
                return Task.FromResult(Respuesta<SalaDto>.Fail("El rol solo puede elegirse dentro del lobby."));
            }

            contexto.SalaPendiente = true;
            return Task.FromResult(Respuesta<SalaDto>.Ok(SalaDto.Crear(contexto.Sala)));
        }, notificar: true);
    }

    public Task<Respuesta<SalaDto>> IniciarPartidaAsync(string codigoSala, int idUsuario)
    {
        return EjecutarEnSalaAsync(codigoSala, async contexto =>
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);

            if (contexto.Sala.IdAnfitrion != idUsuario)
            {
                return Respuesta<SalaDto>.Fail("Solo el anfitrión puede iniciar la partida.");
            }

            // Repetir la solicitud confirmada no crea otro motor ni otra serie.
            if (contexto.Sala.Estado == EstadoSala.Jugando)
            {
                return Respuesta<SalaDto>.Ok(SalaDto.Crear(contexto.Sala));
            }

            if (contexto.InicioPendiente == null)
            {
                if (!contexto.Sala.PuedeIniciar)
                {
                    return Respuesta<SalaDto>.Fail("La sala no está lista para iniciar.");
                }

                contexto.InicioPendiente = await PrepararInicioAsync(contexto.Sala);
            }

            await ConfirmarInicioAsync(contexto);
            return Respuesta<SalaDto>.Ok(SalaDto.Crear(contexto.Sala));
        }, notificar: true);
    }

    public Task<Respuesta<NivelDto>> ObtenerNivelAsync(string codigoSala)
    {
        return EjecutarEnSalaAsync(codigoSala, contexto =>
        {
            if (contexto.Sala.Partida == null)
            {
                return Task.FromResult(Respuesta<NivelDto>.Fail("La sala todavía no tiene un nivel iniciado."));
            }

            return Task.FromResult(Respuesta<NivelDto>.Ok(NivelDto.Crear(contexto.Sala.Partida)));
        });
    }

    public Task<Respuesta<EstadoPartidaDto>> ObtenerEstadoAsync(string codigoSala)
    {
        return EjecutarEnSalaAsync(codigoSala, contexto =>
        {
            if (contexto.Sala.Partida == null)
            {
                return Task.FromResult(Respuesta<EstadoPartidaDto>.Fail("La sala todavía no tiene un estado de juego."));
            }

            var estado = EstadoPartidaDto.Crear(contexto.Sala.Partida, contexto.Sala);
            return Task.FromResult(Respuesta<EstadoPartidaDto>.Ok(estado));
        });
    }

    public Task<Respuesta<bool>> EnviarDireccionAsync(
        string codigoSala, int idUsuario, string idConexion, DireccionDto direccion)
    {
        return EjecutarEnSalaAsync(codigoSala, contexto =>
        {
            ArgumentNullException.ThrowIfNull(direccion);
            var actor = ObtenerActorVigente(contexto, idUsuario, idConexion);
            bool aceptada = contexto.Nivel!.Motor.RecibirDireccion(actor.IdPersonaje, direccion.Direccion);
            return Task.FromResult(Respuesta<bool>.Ok(aceptada));
        });
    }

    public Task<Respuesta<bool>> UsarObjetoAsync(string codigoSala, int idUsuario, string idConexion)
    {
        return EjecutarEnSalaAsync(codigoSala, contexto =>
        {
            var actor = ObtenerActorVigente(contexto, idUsuario, idConexion);
            bool aceptado = contexto.Nivel!.Motor.SolicitarUsarObjeto(actor.IdPersonaje);
            return Task.FromResult(Respuesta<bool>.Ok(aceptado));
        });
    }

    private async Task<InicioSala> PrepararInicioAsync(Sala original)
    {
        var candidata = CopiarLobby(original);
        var nivel = await CargarNivelAsync(candidata, 1, FechaActual());

        if (!candidata.IniciarPartida(candidata.IdAnfitrion!.Value, nivel.Motor.Partida, nivel.FechaInicio))
        {
            throw new InvalidOperationException("El nivel preparado no corresponde a la sala.");
        }

        var datos = new TPartida
        {
            CodigoSala = candidata.Codigo,
            Modo = ModosPartida.ACodigo(candidata.Modo!.Value),
            IdDificultad = candidata.Configuracion.IdDificultad,
            Estado = "EN_CURSO",
            FechaInicio = nivel.FechaInicio
        };

        return new InicioSala(candidata, nivel, datos);
    }

    private async Task ConfirmarInicioAsync(ContextoSala contexto)
    {
        var inicio = contexto.InicioPendiente!;
        await using var ambito = _ambitos.CreateAsyncScope();
        var servicio = ambito.ServiceProvider.GetRequiredService<IPartidaLN>();
        var respuesta = await servicio.IniciarAsync(CopiarPartida(inicio.Datos));
        var guardada = ExigirDatos(respuesta, "No se pudo guardar el inicio de la serie");

        if (guardada.IdPartida <= 0 || guardada.CodigoSala != inicio.Datos.CodigoSala ||
            guardada.FechaInicio != inicio.Datos.FechaInicio || guardada.Estado != "EN_CURSO")
        {
            throw new InvalidOperationException("La persistencia no confirmó el inicio solicitado.");
        }

        if (!inicio.SalaPreparada.RegistrarIdPartida(guardada.IdPartida))
        {
            throw new InvalidOperationException("No se pudo asociar el identificador de la partida.");
        }

        contexto.Sala = inicio.SalaPreparada;
        contexto.Nivel = inicio.Nivel;
        contexto.InicioPendiente = null;
        contexto.UltimoFallo = null;
        contexto.UltimoAvance = _tiempo.GetTimestamp();
        MarcarNivelNuevo(contexto);
    }

    private async Task<NivelEnSala> CargarNivelAsync(Sala sala, int numero, DateTime fechaInicio)
    {
        await using var ambito = _ambitos.CreateAsyncScope();
        var catalogos = ambito.ServiceProvider;
        var servicioNiveles = catalogos.GetRequiredService<INivelLN>();
        var niveles = ExigirDatos(await servicioNiveles.ListarAsync(), "No se pudo consultar el catálogo de niveles");
        var datos = niveles.SingleOrDefault(nivel => nivel.Numero == numero);

        if (datos == null)
        {
            throw new InvalidOperationException($"No está disponible el nivel número {numero}.");
        }

        var cargado = _niveles.Cargar(datos);
        var servicioDificultad = catalogos.GetRequiredService<IDificultadLN>();
        var respuestaDificultad = await servicioDificultad.ObtenerAsync(
            new TDificultad { IdDificultad = sala.Configuracion.IdDificultad });
        var dificultad = ExigirDatos(respuestaDificultad, "No se pudo cargar la dificultad");
        var dificultadPropia = new TDificultad
        {
            IdDificultad = dificultad.IdDificultad,
            Nombre = dificultad.Nombre,
            MultiplicadorVelocidadIa = dificultad.MultiplicadorVelocidadIa,
            DuracionAsustadoSeg = dificultad.DuracionAsustadoSeg
        };
        var servicioObjetos = catalogos.GetRequiredService<IObjetoLN>();
        var objetos = ExigirDatos(await servicioObjetos.ListarAsync(), "No se pudo cargar el catálogo de objetos");
        var objetosPropios = objetos.Select(objeto => new TObjeto
        {
            IdObjeto = objeto.IdObjeto,
            Nombre = objeto.Nombre,
            Bando = objeto.Bando,
            Efecto = objeto.Efecto,
            DuracionSeg = objeto.DuracionSeg
        }).ToList();
        var estado = _personajes.CrearNivel(sala, cargado);
        var motor = new MotorPartida(estado, dificultadPropia, cargado.VelocidadBase!.Value, objetosPropios);

        return new NivelEnSala(motor, cargado.IdNivel!.Value, fechaInicio);
    }

    private async Task EjecutarCicloAsync(ContextoSala contexto)
    {
        try
        {
            while (await contexto.Temporizador.WaitForNextTickAsync(_cancelacion))
            {
                await contexto.Compuerta.WaitAsync(_cancelacion);

                try
                {
                    if (contexto.Retirada || _cancelacion.IsCancellationRequested)
                    {
                        break;
                    }

                    bool reintentar = contexto.UltimoFallo == null ||
                        _tiempo.GetElapsedTime(contexto.UltimoFallo.Value) >= IntervaloReintento;

                    if (reintentar)
                    {
                        try
                        {
                            await ProcesarSalaAsync(contexto);
                            contexto.UltimoFallo = null;
                        }
                        catch (Exception excepcion)
                        {
                            contexto.UltimoFallo = _tiempo.GetTimestamp();
                            _logger.LogWarning(excepcion,
                                "Sala {CodigoSala}: se conserva la operación pendiente para reintentar.", contexto.Sala.Codigo);
                        }
                    }

                    await NotificarPendientesAsync(contexto);
                    RetirarSiVacia(contexto);
                }
                catch (Exception excepcion)
                {
                    contexto.UltimoFallo = _tiempo.GetTimestamp();
                    _logger.LogError(excepcion,
                        "Error al procesar el ciclo de la sala {CodigoSala}.", contexto.Sala.Codigo);
                }
                finally
                {
                    contexto.Compuerta.Release();
                }
            }
        }
        catch (OperationCanceledException) when (_cancelacion.IsCancellationRequested)
        {
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion,
                "Se detuvo el ciclo de la sala {CodigoSala}.", contexto.Sala.Codigo);
        }
        finally
        {
            contexto.Temporizador.Dispose();

            lock (_indices)
            {
                _ciclos.Remove(contexto);
            }
        }
    }

    private async Task ProcesarSalaAsync(ContextoSala contexto)
    {
        if (contexto.InicioPendiente != null)
        {
            await ConfirmarInicioAsync(contexto);
        }

        if (contexto.Nivel == null)
        {
            return;
        }

        var motor = contexto.Nivel.Motor;

        if (contexto.Sala.Estado == EstadoSala.Jugando &&
            (!motor.NivelTerminado || !motor.Partida.BonificacionOtorgada))
        {
            long ahora = _tiempo.GetTimestamp();
            var transcurrido = _tiempo.GetElapsedTime(contexto.UltimoAvance, ahora);
            contexto.UltimoAvance = ahora;
            var tiempoAnterior = motor.Partida.TiempoSimulacion;
            var eventos = motor.Avanzar(transcurrido);

            // El temporizador no es exacto: si no alcanzó a correr un paso, el estado
            // sería igual al anterior y el cliente lo descartaría
            if (motor.Partida.TiempoSimulacion != tiempoAnterior || eventos.Count > 0 || motor.NivelTerminado)
            {
                contexto.Eventos = eventos;
                contexto.EstadoPendiente = true;
            }
        }

        if (motor.NivelTerminado || contexto.Sala.Estado == EstadoSala.Finalizada)
        {
            await GuardarRondaAsync(contexto);

            if (contexto.Sala.Estado == EstadoSala.Jugando)
            {
                if (!contexto.Sala.TerminarNivel())
                {
                    throw new InvalidOperationException("No se pudo terminar el nivel de la sala.");
                }

                contexto.TransicionDesde = _tiempo.GetTimestamp();
                contexto.SalaPendiente = true;
            }
        }

        if (contexto.Sala.Estado == EstadoSala.Transicion &&
            _tiempo.GetElapsedTime(contexto.TransicionDesde!.Value) >= ReglasJuego.DuracionTransicion)
        {
            await IniciarSiguienteNivelAsync(contexto);
        }

        if (contexto.Sala.Estado == EstadoSala.Finalizada)
        {
            await GuardarCierreAsync(contexto);
        }
    }

    private async Task GuardarRondaAsync(ContextoSala contexto)
    {
        var nivel = contexto.Nivel!;

        if (nivel.RondaGuardada)
        {
            return;
        }

        if (nivel.RondaPendiente == null)
        {
            nivel.RondaPendiente = nivel.Motor.CrearRonda(
                contexto.Sala.IdPartida!.Value, nivel.IdNivel, nivel.FechaInicio, FechaActual(nivel.FechaInicio));
            nivel.ParticipantesPendientes = nivel.Motor.ObtenerParticipantes();
        }

        await using var ambito = _ambitos.CreateAsyncScope();
        var servicio = ambito.ServiceProvider.GetRequiredService<IPartidaLN>();
        var respuesta = await servicio.GuardarRondaAsync(
            CopiarRonda(nivel.RondaPendiente), CopiarParticipantes(nivel.ParticipantesPendientes!));
        var ronda = ExigirDatos(respuesta, "No se pudo guardar la ronda");

        if (ronda.IdRonda <= 0 || ronda.IdPartida != contexto.Sala.IdPartida || ronda.IdNivel != nivel.IdNivel)
        {
            throw new InvalidOperationException("La persistencia no confirmó la ronda solicitada.");
        }

        nivel.RondaPendiente.IdRonda = ronda.IdRonda;
        nivel.RondaGuardada = true;
    }

    private async Task IniciarSiguienteNivelAsync(ContextoSala contexto)
    {
        var siguiente = await CargarNivelAsync(
            contexto.Sala, contexto.Sala.NumeroNivel + 1, FechaActual(contexto.Nivel!.RondaPendiente!.FechaFin));

        if (!contexto.Sala.IniciarSiguienteNivel(siguiente.Motor.Partida))
        {
            throw new InvalidOperationException("La sala rechazó el siguiente nivel.");
        }

        contexto.Nivel = siguiente;
        contexto.TransicionDesde = null;
        contexto.UltimoAvance = _tiempo.GetTimestamp();
        MarcarNivelNuevo(contexto);
    }

    private async Task GuardarCierreAsync(ContextoSala contexto)
    {
        if (contexto.CierreGuardado)
        {
            return;
        }

        contexto.CierrePendiente ??= new TPartida
        {
            IdPartida = contexto.Sala.IdPartida!.Value,
            CodigoSala = contexto.Sala.Codigo,
            Modo = ModosPartida.ACodigo(contexto.Sala.Modo!.Value),
            IdDificultad = contexto.Sala.Configuracion.IdDificultad,
            FechaInicio = contexto.Sala.FechaInicio!.Value,
            FechaFin = FechaActual(contexto.Nivel!.RondaPendiente!.FechaFin),
            Estado = contexto.Abandonada ? "ABANDONADA" : "FINALIZADA"
        };

        await using var ambito = _ambitos.CreateAsyncScope();
        var servicio = ambito.ServiceProvider.GetRequiredService<IPartidaLN>();
        var respuesta = await servicio.FinalizarAsync(CopiarPartida(contexto.CierrePendiente));
        var guardada = ExigirDatos(respuesta, "No se pudo cerrar la serie");

        if (guardada.IdPartida != contexto.Sala.IdPartida || guardada.Estado != contexto.CierrePendiente.Estado)
        {
            throw new InvalidOperationException("La persistencia no confirmó el cierre solicitado.");
        }

        contexto.CierreGuardado = true;
        contexto.SalaPendiente = true;
        contexto.EstadoPendiente = true;
    }

    private async Task NotificarPendientesAsync(ContextoSala contexto)
    {
        string codigo = contexto.Sala.Codigo;

        if (contexto.SalaPendiente)
        {
            var sala = SalaDto.Crear(contexto.Sala);
            contexto.SalaPendiente = !await IntentarNotificarAsync(() => _notificador.NotificarSalaAsync(codigo, sala));
        }

        if (contexto.NivelPendiente && contexto.Sala.Partida != null)
        {
            var nivel = NivelDto.Crear(contexto.Sala.Partida);
            contexto.NivelPendiente = !await IntentarNotificarAsync(() => _notificador.NotificarNivelAsync(codigo, nivel));
        }

        if (contexto.EstadoPendiente && contexto.Sala.Partida != null)
        {
            var estado = EstadoPartidaDto.Crear(contexto.Sala.Partida, contexto.Sala, contexto.Eventos);
            contexto.EstadoPendiente = !await IntentarNotificarAsync(() => _notificador.NotificarEstadoAsync(codigo, estado));

            if (!contexto.EstadoPendiente)
            {
                contexto.Eventos = Array.Empty<EventoJuego>();
            }
        }
    }

    private async Task EnviarInstantaneasAsync(ContextoSala contexto, string idConexion)
    {
        var sala = SalaDto.Crear(contexto.Sala);
        await IntentarNotificarAsync(() => _notificador.EnviarSalaAConexionAsync(idConexion, sala));

        if (contexto.Sala.Partida != null)
        {
            var nivel = NivelDto.Crear(contexto.Sala.Partida);
            var estado = EstadoPartidaDto.Crear(contexto.Sala.Partida, contexto.Sala);
            await IntentarNotificarAsync(() => _notificador.EnviarNivelAConexionAsync(idConexion, nivel));
            await IntentarNotificarAsync(() => _notificador.EnviarEstadoAConexionAsync(idConexion, estado));
        }
    }

    private async Task<bool> IntentarNotificarAsync(Func<Task> notificar)
    {
        try
        {
            await notificar();
            return true;
        }
        catch (Exception excepcion)
        {
            _logger.LogWarning(excepcion, "No se pudo enviar una instantánea del juego.");
            return false;
        }
    }

    private async Task<Respuesta<T>> EjecutarEnSalaAsync<T>(
        string codigo, Func<ContextoSala, Task<Respuesta<T>>> operacion, bool notificar = false)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
            string normalizado = codigo.Trim().ToUpperInvariant();
            ContextoSala contexto;

            lock (_indices)
            {
                VerificarDisponible();

                if (!_salas.TryGetValue(normalizado, out contexto!))
                {
                    return Respuesta<T>.Fail("Sala no encontrada.");
                }
            }

            return await EjecutarEnSalaAsync(contexto, operacion, notificar);
        }
        catch (Exception excepcion)
        {
            return Fallo<T>(excepcion);
        }
    }

    private async Task<Respuesta<T>> EjecutarEnSalaAsync<T>(
        ContextoSala contexto, Func<ContextoSala, Task<Respuesta<T>>> operacion, bool notificar = false)
    {
        bool entro = false;

        try
        {
            await contexto.Compuerta.WaitAsync(_cancelacion);
            entro = true;

            lock (_indices)
            {
                VerificarDisponible();

                if (contexto.Retirada)
                {
                    return Respuesta<T>.Fail("La sala ya fue retirada.");
                }
            }

            var resultado = await operacion(contexto);

            if (notificar)
            {
                await NotificarPendientesAsync(contexto);
            }

            RetirarSiVacia(contexto);
            return resultado;
        }
        catch (Exception excepcion)
        {
            return Fallo<T>(excepcion);
        }
        finally
        {
            if (entro)
            {
                contexto.Compuerta.Release();
            }
        }
    }

    private void RetirarSiVacia(ContextoSala contexto)
    {
        bool puedeRetirarse = contexto.InicioPendiente == null &&
            (contexto.Nivel == null || contexto.CierreGuardado);

        if (!contexto.Sala.Vacia || !puedeRetirarse)
        {
            return;
        }

        lock (_indices)
        {
            contexto.Retirada = true;
            _salas.Remove(contexto.Sala.Codigo);
        }

        contexto.Temporizador.Dispose();
    }

    private void VerificarDisponible()
    {
        if (_cerrando)
        {
            throw new InvalidOperationException("El gestor de salas se está cerrando.");
        }
    }

    private void VerificarIdentidadesLibres(int idUsuario, string idConexion)
    {
        if (_usuarios.ContainsKey(idUsuario))
        {
            throw new InvalidOperationException("El usuario ya tiene una plaza en otra sala.");
        }

        if (_conexiones.ContainsKey(idConexion))
        {
            throw new InvalidOperationException("La conexión ya está registrada en otra sala.");
        }
    }

    private string CrearCodigo()
    {
        string codigo;

        do
        {
            var caracteres = new char[6];

            for (int indice = 0; indice < caracteres.Length; indice++)
            {
                caracteres[indice] = CaracteresCodigo[RandomNumberGenerator.GetInt32(CaracteresCodigo.Length)];
            }

            codigo = new string(caracteres);
        }
        while (_salas.ContainsKey(codigo));

        return codigo;
    }

    private static void ValidarIngreso(int idUsuario, string nombreUsuario, string idConexion)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreUsuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);
    }

    private static void VerificarSinInicioPendiente(ContextoSala contexto)
    {
        if (contexto.InicioPendiente != null)
        {
            throw new InvalidOperationException("El inicio está pendiente de guardar; se reintentará con los mismos datos.");
        }
    }

    private static void VerificarAnfitrionEnLobby(ContextoSala contexto, int idUsuario)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
        VerificarSinInicioPendiente(contexto);

        if (contexto.Sala.Estado != EstadoSala.Lobby || contexto.Sala.IdAnfitrion != idUsuario)
        {
            throw new InvalidOperationException("Solo el anfitrión puede configurar una sala en el lobby.");
        }
    }

    private static Entidad ObtenerActorVigente(ContextoSala contexto, int idUsuario, string idConexion)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);
        var jugador = contexto.Sala.Jugadores.FirstOrDefault(actual => actual.IdUsuario == idUsuario);

        if (contexto.Sala.Estado != EstadoSala.Jugando || contexto.Nivel == null ||
            contexto.Nivel.Motor.NivelTerminado || jugador == null ||
            !jugador.EstaConectado || jugador.IdConexion != idConexion)
        {
            throw new InvalidOperationException("El usuario no tiene una conexión vigente para controlar esta partida.");
        }

        var actor = contexto.Nivel.Motor.Partida.ObtenerActorDeUsuario(idUsuario);

        if (actor == null || actor.IdPersonaje != jugador.IdPersonaje || actor.ControlIa || !actor.PuedeParticipar)
        {
            throw new InvalidOperationException("El usuario no puede controlar ese personaje en este momento.");
        }

        return actor;
    }

    private static void MarcarCambioDeSala(ContextoSala contexto)
    {
        contexto.SalaPendiente = true;
        contexto.EstadoPendiente = contexto.Nivel != null;
    }

    private static bool SerieCompleta(ContextoSala contexto)
    {
        return contexto.Sala.NumeroNivel == ConfiguracionPartida.CantidadNiveles &&
            contexto.Nivel?.Motor.NivelTerminado == true;
    }

    private static void MarcarNivelNuevo(ContextoSala contexto)
    {
        MarcarCambioDeSala(contexto);
        contexto.NivelPendiente = true;
        contexto.Eventos = Array.Empty<EventoJuego>();
    }

    private DateTime FechaActual(DateTime minima = default)
    {
        var ahora = _tiempo.GetUtcNow().UtcDateTime;
        return ahora >= minima ? ahora : minima;
    }

    private static T ExigirDatos<T>(Respuesta<T> respuesta, string mensaje)
    {
        if (!respuesta.Success || respuesta.Data == null)
        {
            throw new InvalidOperationException($"{mensaje}: {respuesta.Error}");
        }

        return respuesta.Data;
    }

    private Respuesta<T> Fallo<T>(Exception excepcion)
    {
        if (excepcion is OperationCanceledException && _cancelacion.IsCancellationRequested)
        {
            return Respuesta<T>.Fail("El gestor de salas se está cerrando.");
        }

        _logger.LogDebug(excepcion, "No se pudo completar una operación de salas.");
        return Respuesta<T>.Fail(excepcion.Message);
    }

    private static Sala CopiarLobby(Sala original)
    {
        var copia = new Sala(original.Codigo);

        foreach (var jugador in original.Jugadores)
        {
            copia.Ingresar(jugador.IdUsuario, jugador.NombreUsuario, jugador.IdConexion);
            copia.ElegirRol(jugador.IdUsuario, jugador.RolPreferido);

            if (!jugador.EstaConectado)
            {
                copia.Desconectar(jugador.IdConexion);
            }
        }

        copia.Configurar(copia.IdAnfitrion!.Value, original.Configuracion);
        return copia;
    }

    private static TPartida CopiarPartida(TPartida datos)
    {
        return new TPartida
        {
            IdPartida = datos.IdPartida,
            CodigoSala = datos.CodigoSala,
            Modo = datos.Modo,
            IdDificultad = datos.IdDificultad,
            Estado = datos.Estado,
            FechaInicio = datos.FechaInicio,
            FechaFin = datos.FechaFin
        };
    }

    private static TRonda CopiarRonda(TRonda datos)
    {
        return new TRonda
        {
            IdRonda = datos.IdRonda,
            IdPartida = datos.IdPartida,
            IdNivel = datos.IdNivel,
            BandoGanador = datos.BandoGanador,
            FechaInicio = datos.FechaInicio,
            FechaFin = datos.FechaFin
        };
    }

    private static IReadOnlyList<TParticipanteRonda> CopiarParticipantes(
        IReadOnlyList<TParticipanteRonda> participantes)
    {
        return participantes.Select(datos => new TParticipanteRonda
        {
            IdParticipante = datos.IdParticipante,
            IdRonda = datos.IdRonda,
            IdUsuario = datos.IdUsuario,
            IdPersonaje = datos.IdPersonaje,
            EsIa = datos.EsIa,
            Puntuacion = datos.Puntuacion,
            FantasmasComidos = datos.FantasmasComidos,
            PacmansAtrapados = datos.PacmansAtrapados,
            ObjetosUsados = datos.ObjetosUsados,
            Abandono = datos.Abandono
        }).ToList().AsReadOnly();
    }

    public ValueTask DisposeAsync()
    {
        lock (_indices)
        {
            if (_disposicion != null)
            {
                return new ValueTask(_disposicion);
            }

            _cerrando = true;
            _cierre.Cancel();
            var contextos = _salas.Values.Concat(_ciclos).Distinct().ToArray();

            foreach (var contexto in contextos)
            {
                contexto.Temporizador.Dispose();
            }

            _disposicion = Task.Run(() => CerrarGestorAsync(contextos));
            return new ValueTask(_disposicion);
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private async Task CerrarGestorAsync(IReadOnlyList<ContextoSala> contextos)
    {
        var errores = new List<Exception>();

        try
        {
            await Task.WhenAll(contextos.Select(contexto => contexto.Ciclo));

            foreach (var contexto in contextos)
            {
                await contexto.Compuerta.WaitAsync();

                try
                {
                    if (contexto.InicioPendiente != null)
                    {
                        await ConfirmarInicioAsync(contexto);
                    }

                    if (contexto.Nivel != null && !contexto.CierreGuardado)
                    {
                        if (contexto.Sala.Estado != EstadoSala.Finalizada)
                        {
                            contexto.Abandonada = !SerieCompleta(contexto);
                            contexto.Sala.Finalizar();
                        }

                        await GuardarRondaAsync(contexto);
                        await GuardarCierreAsync(contexto);
                        await NotificarPendientesAsync(contexto);
                    }
                }
                catch (Exception excepcion)
                {
                    errores.Add(excepcion);
                    _logger.LogError(excepcion, "No se pudo guardar el cierre de {CodigoSala}.", contexto.Sala.Codigo);
                }
                finally
                {
                    contexto.Compuerta.Release();
                }
            }
        }
        finally
        {
            lock (_indices)
            {
                _salas.Clear();
                _usuarios.Clear();
                _conexiones.Clear();
            }

            _cierre.Dispose();
        }

        if (errores.Count > 0)
        {
            throw new AggregateException("El gestor cerró sus ciclos, pero quedaron resultados sin confirmar.", errores);
        }
    }
}
