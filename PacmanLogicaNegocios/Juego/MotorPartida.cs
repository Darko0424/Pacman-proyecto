using PacmanDominio.EntidadesTipadas;
using PacmanDominio.Juego;
using PacmanDominio.Juego.Interfaces;
using PacmanLogicaNegocios.Juego.Controladores;
using PacmanLogicaNegocios.Juego.Efectos;
using PacmanLogicaNegocios.Juego.Sistemas;

namespace PacmanLogicaNegocios.Juego;

// Simula un nivel. Se crea uno por cada nivel que empieza (con el EstadoPartida
// que acepta la Sala) y se descarta al terminar el nivel.
//
// No es seguro para varios hilos: el gestor de salas debe llamar a sus métodos
// de forma serializada (por ejemplo, con el mismo candado que protege la sala).
public sealed class MotorPartida
{
    private readonly ContextoMotor _contexto;
    private readonly ControladorIA _controladorIa;
    private readonly Dictionary<byte, ControladorHumano> _humanos = new();
    private TimeSpan _pendiente = TimeSpan.Zero;

    // Efectos de los objetos provisionales; para un objeto nuevo, agregar su clase aquí
    public static IReadOnlyList<IEfectoObjeto> EfectosPredeterminados { get; } = new IEfectoObjeto[]
    {
        new EfectoVelocidad(),
        new EfectoEscudo(),
        new EfectoCongelar()
    };

    public EstadoPartida Partida
    {
        get
        {
            return _contexto.Partida;
        }
    }

    public bool NivelTerminado
    {
        get
        {
            return Partida.Terminada;
        }
    }

    // dificultad: la de partida.Configuracion.IdDificultad (catálogo).
    // velocidadNivel: TNivel.VelocidadBase del nivel que se juega.
    // objetos: el catálogo completo; se descartan los que no tienen efecto implementado.
    // semilla: para repetir exactamente una simulación (pruebas); null = al azar.
    public MotorPartida(
        EstadoPartida partida,
        TDificultad dificultad,
        decimal velocidadNivel,
        IEnumerable<TObjeto> objetos,
        IEnumerable<IEfectoObjeto>? efectos = null,
        int? semilla = null)
    {
        ArgumentNullException.ThrowIfNull(partida);
        ArgumentNullException.ThrowIfNull(dificultad);
        ArgumentNullException.ThrowIfNull(objetos);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(velocidadNivel);

        if (dificultad.IdDificultad != partida.Configuracion.IdDificultad)
        {
            throw new ArgumentException(
                "La dificultad no es la de la configuración de la partida.", nameof(dificultad));
        }

        if (dificultad.MultiplicadorVelocidadIa <= 0)
        {
            throw new ArgumentException(
                "La dificultad debe tener un multiplicador de velocidad positivo.", nameof(dificultad));
        }

        var implementaciones = new Dictionary<string, IEfectoObjeto>(StringComparer.Ordinal);

        foreach (var efecto in efectos ?? EfectosPredeterminados)
        {
            if (!implementaciones.TryAdd(efecto.TipoEfecto, efecto))
            {
                throw new ArgumentException(
                    $"El efecto {efecto.TipoEfecto} está repetido.", nameof(efectos));
            }
        }

        var objetosUsables = objetos
            .Where(objeto => objeto != null &&
                implementaciones.ContainsKey(objeto.Efecto) &&
                CatalogoPersonajes.DesdeCodigo(objeto.Bando) != null &&
                objeto.DuracionSeg > 0)
            .ToList()
            .AsReadOnly();

        _contexto = new ContextoMotor(
            partida,
            dificultad,
            (double)velocidadNivel,
            implementaciones,
            objetosUsables,
            semilla.HasValue ? new Random(semilla.Value) : new Random());
        _controladorIa = new ControladorIA(_contexto);

        foreach (var actor in partida.Actores.Where(actor => !actor.EsIa))
        {
            _humanos[actor.IdPersonaje] = new ControladorHumano();
        }
    }

    // Dirección del celular de un jugador. Se ignora si su personaje lo mueve la IA.
    public bool RecibirDireccion(byte idPersonaje, Direccion direccion)
    {
        if (!Enum.IsDefined(direccion) || !_humanos.TryGetValue(idPersonaje, out var controlador))
        {
            return false;
        }

        controlador.RecibirDireccion(direccion);

        return true;
    }

    // Botón de acción: el objeto se usa en el próximo tick
    public bool SolicitarUsarObjeto(byte idPersonaje)
    {
        if (!_humanos.TryGetValue(idPersonaje, out var controlador))
        {
            return false;
        }

        controlador.SolicitarUsarObjeto();

        return true;
    }

    // Avanza la simulación el tiempo transcurrido, en pasos fijos de
    // ReglasJuego.DuracionTick (el resto se acumula para la siguiente llamada).
    // Devuelve lo que pasó, para enviarlo junto con el estado.
    public IReadOnlyList<EventoJuego> Avanzar(TimeSpan transcurrido)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(transcurrido, TimeSpan.Zero);

        _pendiente += transcurrido;

        while (_pendiente >= ReglasJuego.DuracionTick && !NivelTerminado)
        {
            _pendiente -= ReglasJuego.DuracionTick;
            EjecutarTick();
        }

        if (NivelTerminado)
        {
            // Se otorga una sola vez, aunque el nivel haya terminado fuera del motor
            SistemaPuntaje.CerrarNivel(_contexto);
            _pendiente = TimeSpan.Zero;
        }

        var eventos = _contexto.Eventos.ToList().AsReadOnly();
        _contexto.Eventos.Clear();

        return eventos;
    }

    // Al terminar el nivel: los participantes para PartidaLN.GuardarRondaAsync
    public IReadOnlyList<TParticipanteRonda> ObtenerParticipantes()
    {
        return Partida.Actores
            .Select(actor => new TParticipanteRonda
            {
                IdUsuario = actor.IdUsuario,
                IdPersonaje = actor.IdPersonaje,
                EsIa = actor.EsIa,
                Puntuacion = actor.Puntuacion,
                FantasmasComidos = actor.FantasmasComidos,
                PacmansAtrapados = actor.PacmansAtrapados,
                ObjetosUsados = actor.ObjetosUsados,
                Abandono = actor.Abandono
            })
            .OrderBy(participante => participante.IdPersonaje)
            .ToList()
            .AsReadOnly();
    }

    // La ronda para PartidaLN.GuardarRondaAsync; sin ganador si el nivel se interrumpió
    public TRonda CrearRonda(int idPartida, int idNivel, DateTime fechaInicio, DateTime fechaFin)
    {
        var ganador = Partida.BandoGanador;

        return new TRonda
        {
            IdPartida = idPartida,
            IdNivel = idNivel,
            BandoGanador = ganador.HasValue ? CatalogoPersonajes.ACodigo(ganador.Value) : null,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin
        };
    }

    private void EjecutarTick()
    {
        var partida = Partida;
        var tiempo = partida.TiempoSimulacion + ReglasJuego.DuracionTick;

        partida.ActualizarTiempo(tiempo);

        SistemaObjetos.VencerEfectos(_contexto);
        SistemaReaparicion.Procesar(_contexto);

        var previas = partida.Actores.ToDictionary(actor => actor.IdPersonaje, actor => actor.Posicion);
        var decisiones = new List<(byte IdPersonaje, Direccion Direccion)>();

        foreach (var actor in partida.Actores.Where(actor => actor.PuedeParticipar).ToList())
        {
            IControlador controlador = actor.ControlIa ||
                !_humanos.TryGetValue(actor.IdPersonaje, out var humano)
                    ? _controladorIa
                    : humano;

            var decision = controlador.ObtenerDecision(actor, partida, tiempo);

            if (decision.UsarObjeto)
            {
                SistemaObjetos.Usar(_contexto, actor);
            }

            decisiones.Add((actor.IdPersonaje, decision.Direccion));
        }

        foreach (var (idPersonaje, direccion) in decisiones)
        {
            SistemaMovimiento.Mover(
                _contexto, partida.ObtenerActor(idPersonaje), direccion, ReglasJuego.DuracionTick);
        }

        SistemaColisiones.Resolver(_contexto, previas);
        SistemaPuntaje.ComerBolas(_contexto);
        SistemaObjetos.RecogerCajas(_contexto);
        SistemaObjetos.AparecerCajas(_contexto);

        if (partida.Terminada)
        {
            SistemaPuntaje.CerrarNivel(_contexto);
        }
    }
}
