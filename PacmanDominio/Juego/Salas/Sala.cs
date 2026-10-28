namespace PacmanDominio.Juego.Salas;

public enum EstadoSala
{
    // Esperando jugadores; el anfitrión configura e inicia
    Lobby = 1,
    Jugando = 2,
    // Vuelta breve al lobby entre niveles: resultados y nuevos roles
    Transicion = 3,
    Finalizada = 4
}

// Una sala juega una serie de niveles. El gestor serializa sus acciones; los
// estados de cada nivel los construye la lógica y aquí solo se validan.
public sealed class Sala
{
    private const int LargoMaximoCodigo = 6;

    private readonly List<JugadorConectado> _jugadores = new();

    // Los expulsados no pueden volver a entrar a esta sala
    private readonly HashSet<int> _expulsados = new();

    public string Codigo { get; }

    // En orden de ingreso: si el anfitrión se va, lo reemplaza el siguiente
    public IReadOnlyList<JugadorConectado> Jugadores { get; }

    public int? IdAnfitrion { get; private set; }
    public ConfiguracionSala Configuracion { get; private set; } = ConfiguracionSala.PorDefecto;
    public EstadoSala Estado { get; private set; } = EstadoSala.Lobby;

    // El anfitrión la bloquea para que nadie más entre
    public bool Bloqueada { get; private set; }

    // Se fija al iniciar la serie según cuántos jugadores había
    public ModoPartida? Modo { get; private set; }

    // Nivel en juego, o el que acaba de terminar durante la transición
    public EstadoPartida? Partida { get; private set; }

    public DateTime? FechaInicio { get; private set; }

    // Identificador de la partida guardada en la base de datos
    public int? IdPartida { get; private set; }

    // Contra la IA el jugador conserva su rol en todos los niveles
    public Bando? RolContraIa { get; private set; }

    public int NumeroNivel
    {
        get
        {
            return Partida?.NumeroNivel ?? 0;
        }
    }

    public bool Vacia
    {
        get
        {
            return _jugadores.Count == 0;
        }
    }

    public ModoPartida ModoSegunJugadores
    {
        get
        {
            return _jugadores.Count == 1 ? ModoPartida.ContraIa : ModoPartida.Multijugador;
        }
    }

    // Los desconectados no bloquean el inicio: la IA mueve su personaje hasta que vuelvan
    public bool PuedeIniciar
    {
        get
        {
            var anfitrion = BuscarJugador(IdAnfitrion);

            return Estado == EstadoSala.Lobby &&
                _jugadores.Count >= 1 &&
                _jugadores.Count <= ConfiguracionPartida.CantidadMaximaHumanos &&
                anfitrion != null && anfitrion.EstaConectado;
        }
    }

    public Sala(string codigo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);

        if (codigo.Length > LargoMaximoCodigo)
        {
            throw new ArgumentException(
                "El código de sala admite como máximo seis caracteres.",
                nameof(codigo));
        }

        Codigo = codigo;
        Jugadores = _jugadores.AsReadOnly();
    }

    // El primero en entrar a una sala sin anfitrión (quien la crea) pasa a serlo
    public bool Ingresar(int idUsuario, string nombreUsuario, string idConexion)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreUsuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);

        if (Estado != EstadoSala.Lobby || Bloqueada || _expulsados.Contains(idUsuario) ||
            _jugadores.Count >= ConfiguracionPartida.CantidadMaximaHumanos)
        {
            return false;
        }

        if (_jugadores.Any(jugador =>
            jugador.IdUsuario == idUsuario || jugador.IdConexion == idConexion))
        {
            return false;
        }

        _jugadores.Add(new JugadorConectado(idUsuario, nombreUsuario, idConexion));
        IdAnfitrion ??= idUsuario;

        return true;
    }

    public bool Configurar(int idUsuario, ConfiguracionSala configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        if (Estado != EstadoSala.Lobby || idUsuario != IdAnfitrion)
        {
            return false;
        }

        Configuracion = configuracion;

        return true;
    }

    // Solo el anfitrión, en el lobby
    public bool Bloquear(int idUsuario, bool bloqueada)
    {
        if (Estado != EstadoSala.Lobby || idUsuario != IdAnfitrion)
        {
            return false;
        }

        Bloqueada = bloqueada;

        return true;
    }

    // Solo el anfitrión, en el lobby, y no a sí mismo
    public bool Expulsar(int idAnfitrion, int idUsuario)
    {
        if (Estado != EstadoSala.Lobby || idAnfitrion != IdAnfitrion ||
            idUsuario == idAnfitrion || BuscarJugador(idUsuario) == null)
        {
            return false;
        }

        _expulsados.Add(idUsuario);

        return Salir(idUsuario);
    }

    public bool FueExpulsado(int idUsuario)
    {
        return _expulsados.Contains(idUsuario);
    }

    // Preferencia para jugar solo contra la IA; null = que decida el sorteo
    public bool ElegirRol(int idUsuario, Bando? rol)
    {
        if (rol.HasValue && !Enum.IsDefined(rol.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(rol));
        }

        var jugador = BuscarJugador(idUsuario);

        if (Estado != EstadoSala.Lobby || jugador == null)
        {
            return false;
        }

        jugador.ElegirRol(rol);

        return true;
    }

    // Salida voluntaria. En pleno nivel la IA toma su personaje hasta el final del nivel.
    public bool Salir(int idUsuario)
    {
        var jugador = BuscarJugador(idUsuario);

        // También permite vaciar una sala finalizada y liberar su capacidad.
        if (jugador == null)
        {
            return false;
        }

        _jugadores.Remove(jugador);
        jugador.Desconectar();

        if (Estado == EstadoSala.Jugando)
        {
            Partida!.CederControlAIa(idUsuario, abandono: true);
        }

        if (IdAnfitrion == idUsuario)
        {
            IdAnfitrion = _jugadores.FirstOrDefault()?.IdUsuario;
        }

        // Sin humanos no tiene sentido seguir la serie
        if (_jugadores.Count == 0 && Estado != EstadoSala.Lobby)
        {
            Estado = EstadoSala.Finalizada;
        }

        return true;
    }

    // Se conserva la plaza; en pleno nivel la IA mueve su personaje hasta que vuelva.
    public bool Desconectar(string idConexion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);

        var jugador = _jugadores.FirstOrDefault(
            participante => participante.IdConexion == idConexion && participante.EstaConectado);

        if (jugador == null)
        {
            return false;
        }

        jugador.Desconectar();

        if (Estado == EstadoSala.Jugando)
        {
            Partida!.CederControlAIa(jugador.IdUsuario, abandono: false);
        }

        return true;
    }

    public bool Reconectar(int idUsuario, string idConexion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);

        var jugador = BuscarJugador(idUsuario);

        if (jugador == null || Estado == EstadoSala.Finalizada)
        {
            return false;
        }

        if (_jugadores.Any(
            otroJugador => otroJugador.IdUsuario != idUsuario &&
                otroJugador.IdConexion == idConexion))
        {
            return false;
        }

        // Solo queda vigente la nueva conexión, aunque la anterior avise su cierre después.
        jugador.Reconectar(idConexion);

        if (Estado == EstadoSala.Jugando)
        {
            Partida!.RecuperarControl(idUsuario);
        }

        return true;
    }

    // Solo el anfitrión. El primer nivel lo construye la lógica con los roles ya sorteados.
    public bool IniciarPartida(int idUsuario, EstadoPartida primerNivel, DateTime fechaInicio)
    {
        ArgumentNullException.ThrowIfNull(primerNivel);

        if (idUsuario != IdAnfitrion || !PuedeIniciar ||
            primerNivel.NumeroNivel != 1 ||
            primerNivel.Modo != ModoSegunJugadores ||
            !CorrespondeALaSala(primerNivel))
        {
            return false;
        }

        Bando? rolContraIa = null;

        if (primerNivel.Modo == ModoPartida.ContraIa)
        {
            var jugador = _jugadores[0];
            rolContraIa = primerNivel.ObtenerActorDeUsuario(jugador.IdUsuario)!.Bando;

            if (jugador.RolPreferido.HasValue && jugador.RolPreferido != rolContraIa)
            {
                return false;
            }
        }

        // La asociación se realiza al final para no dejar cambios parciales al rechazar el inicio.
        if (!primerNivel.AsociarSala(Codigo))
        {
            return false;
        }

        Modo = primerNivel.Modo;
        RolContraIa = rolContraIa;
        FechaInicio = fechaInicio;
        IdPartida = null;
        AceptarNivel(primerNivel);

        return true;
    }

    // Lo llama la lógica cuando el nivel tiene ganador
    public bool TerminarNivel()
    {
        if (Estado != EstadoSala.Jugando || !Partida!.Terminada)
        {
            return false;
        }

        Estado = NumeroNivel == ConfiguracionPartida.CantidadNiveles
            ? EstadoSala.Finalizada
            : EstadoSala.Transicion;

        return true;
    }

    // Tras la transición: el siguiente nivel, con los roles sorteados de nuevo
    // salvo que la configuración tenga bandos fijos
    public bool IniciarSiguienteNivel(EstadoPartida siguienteNivel)
    {
        ArgumentNullException.ThrowIfNull(siguienteNivel);

        if (Estado != EstadoSala.Transicion ||
            siguienteNivel.NumeroNivel != NumeroNivel + 1 ||
            siguienteNivel.Modo != Modo ||
            !CorrespondeALaSala(siguienteNivel))
        {
            return false;
        }

        if (Modo == ModoPartida.ContraIa &&
            siguienteNivel.ObtenerActorDeUsuario(_jugadores[0].IdUsuario)!.Bando != RolContraIa)
        {
            return false;
        }

        if (Modo == ModoPartida.Multijugador && Configuracion.BandosFijos &&
            _jugadores.Any(jugador =>
                siguienteNivel.ObtenerActorDeUsuario(jugador.IdUsuario)!.IdPersonaje != jugador.IdPersonaje))
        {
            return false;
        }

        if (!siguienteNivel.AsociarSala(Codigo))
        {
            return false;
        }

        AceptarNivel(siguienteNivel);

        return true;
    }

    public bool RegistrarIdPartida(int idPartida)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idPartida);

        if (IdPartida.HasValue || Estado == EstadoSala.Lobby)
        {
            return false;
        }

        IdPartida = idPartida;

        return true;
    }

    // Corta la serie, por ejemplo si el servidor cierra la sala
    public bool Finalizar()
    {
        if (Estado == EstadoSala.Lobby || Estado == EstadoSala.Finalizada)
        {
            return false;
        }

        Estado = EstadoSala.Finalizada;

        return true;
    }

    // Terminada la serie, los que siguen en la sala pueden jugar otra
    public bool VolverAlLobby()
    {
        if (Estado != EstadoSala.Finalizada || _jugadores.Count == 0)
        {
            return false;
        }

        Estado = EstadoSala.Lobby;
        Modo = null;
        Partida = null;
        FechaInicio = null;
        IdPartida = null;
        RolContraIa = null;

        foreach (var jugador in _jugadores)
        {
            jugador.AsignarPersonaje(null);
        }

        return true;
    }

    private void AceptarNivel(EstadoPartida nivel)
    {
        Partida = nivel;
        Estado = EstadoSala.Jugando;

        foreach (var jugador in _jugadores)
        {
            jugador.AsignarPersonaje(nivel.ObtenerActorDeUsuario(jugador.IdUsuario)!.IdPersonaje);

            if (!jugador.EstaConectado)
            {
                nivel.CederControlAIa(jugador.IdUsuario, abandono: false);
            }
        }
    }

    private JugadorConectado? BuscarJugador(int? idUsuario)
    {
        return _jugadores.FirstOrDefault(jugador => jugador.IdUsuario == idUsuario);
    }

    // Cada jugador de la sala controla exactamente un personaje, y no hay otros humanos
    private bool CorrespondeALaSala(EstadoPartida nivel)
    {
        if (nivel.CodigoSala != null || !nivel.EsEstadoInicial ||
            nivel.Configuracion != Configuracion)
        {
            return false;
        }

        var usuariosNivel = nivel.Actores
            .Where(actor => !actor.EsIa)
            .Select(actor => actor.IdUsuario!.Value)
            .ToHashSet();

        return usuariosNivel.SetEquals(_jugadores.Select(jugador => jugador.IdUsuario));
    }
}
