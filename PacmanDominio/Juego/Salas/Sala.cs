using PacmanDominio.Juego.Actores;

namespace PacmanDominio.Juego.Salas;

// El gestor futuro serializará las acciones de esta sala.
public sealed class Sala
{
    private const int LargoMaximoCodigo = 6;
    private const int CantidadMaximaHumanos =
        ConfiguracionPartida.CantidadPacMan + ConfiguracionPartida.CantidadFantasmasHumanos;

    private readonly List<JugadorConectado> _jugadores = new();

    public string Codigo { get; }
    public IReadOnlyList<JugadorConectado> Jugadores { get; }
    public EstadoPartida? Partida { get; private set; }

    public byte? IdPersonajeBot
    {
        get
        {
            if (_jugadores.Count(jugador => EsPersonajeFantasma(jugador.IdPersonaje)) !=
                ConfiguracionPartida.CantidadFantasmasHumanos)
            {
                return null;
            }

            for (byte idPersonaje = 3; idPersonaje <= 5; idPersonaje++)
            {
                if (!_jugadores.Any(jugador => jugador.IdPersonaje == idPersonaje))
                {
                    return idPersonaje;
                }
            }

            return null;
        }
    }

    public bool PartidaIniciada
    {
        get
        {
            return Partida != null;
        }
    }

    public bool PuedeIniciar
    {
        get
        {
            if (PartidaIniciada || _jugadores.Count != CantidadMaximaHumanos)
            {
                return false;
            }

            if (_jugadores.Any(jugador =>
                !jugador.EstaConectado || !jugador.IdPersonaje.HasValue))
            {
                return false;
            }

            return _jugadores.Count(jugador => EsPersonajePacMan(jugador.IdPersonaje)) ==
                    ConfiguracionPartida.CantidadPacMan &&
                _jugadores.Count(jugador => EsPersonajeFantasma(jugador.IdPersonaje)) ==
                    ConfiguracionPartida.CantidadFantasmasHumanos;
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

    public bool Ingresar(int idUsuario, string idConexion)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);

        if (PartidaIniciada || _jugadores.Count >= CantidadMaximaHumanos)
        {
            return false;
        }

        if (_jugadores.Any(jugador =>
            jugador.IdUsuario == idUsuario || jugador.IdConexion == idConexion))
        {
            return false;
        }

        _jugadores.Add(new JugadorConectado(idUsuario, idConexion));

        return true;
    }

    public bool AsignarPersonaje(int idUsuario, byte? idPersonaje)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);

        if (idPersonaje.HasValue &&
            !EsPersonajePacMan(idPersonaje) && !EsPersonajeFantasma(idPersonaje))
        {
            throw new ArgumentOutOfRangeException(
                nameof(idPersonaje), "El personaje no pertenece al catálogo del juego.");
        }

        if (PartidaIniciada)
        {
            return false;
        }

        var jugador = BuscarJugador(idUsuario);

        if (jugador == null)
        {
            return false;
        }

        if (idPersonaje.HasValue && _jugadores.Any(
            otroJugador => otroJugador.IdUsuario != idUsuario &&
                otroJugador.IdPersonaje == idPersonaje))
        {
            return false;
        }

        jugador.AsignarPersonaje(idPersonaje);

        return true;
    }

    public bool Salir(int idUsuario)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);

        if (PartidaIniciada)
        {
            return false;
        }

        var jugador = BuscarJugador(idUsuario);

        if (jugador == null)
        {
            return false;
        }

        _jugadores.Remove(jugador);
        jugador.Desconectar();

        return true;
    }

    public bool Desconectar(string idConexion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);

        var jugador = _jugadores.FirstOrDefault(
            participante => participante.IdConexion == idConexion && participante.EstaConectado);

        if (jugador == null)
        {
            return false;
        }

        // Se conserva la plaza y el personaje; la política de abandono sigue pendiente.
        jugador.Desconectar();

        return true;
    }

    public bool Reconectar(int idUsuario, string idConexion)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);

        var jugador = BuscarJugador(idUsuario);

        if (jugador == null)
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

        return true;
    }

    public bool IniciarPartida(EstadoPartida partida)
    {
        ArgumentNullException.ThrowIfNull(partida);

        if (!PuedeIniciar || !CorrespondeALaSala(partida) || !EsEstadoInicial(partida))
        {
            return false;
        }

        // La asociación se realiza al final para no dejar cambios parciales al rechazar el inicio.
        if (!partida.AsociarSala(Codigo))
        {
            return false;
        }

        Partida = partida;

        return true;
    }

    private JugadorConectado? BuscarJugador(int idUsuario)
    {
        return _jugadores.FirstOrDefault(jugador => jugador.IdUsuario == idUsuario);
    }

    private bool CorrespondeALaSala(EstadoPartida partida)
    {
        if (partida.Pacmans.Any(pacman => !EsPersonajePacMan(pacman.IdPersonaje)) ||
            partida.Fantasmas.Any(fantasma => !EsPersonajeFantasma(fantasma.IdPersonaje)))
        {
            return false;
        }

        var humanos = partida.Pacmans.Cast<Entidad>()
            .Concat(partida.Fantasmas)
            .Where(entidad => !entidad.EsIa)
            .ToList();

        if (humanos.Count != _jugadores.Count || _jugadores.Any(
            jugador => !humanos.Any(actor =>
                actor.IdUsuario == jugador.IdUsuario && actor.IdPersonaje == jugador.IdPersonaje)))
        {
            return false;
        }

        var bots = partida.Fantasmas.Where(fantasma => fantasma.EsIa).ToList();

        return bots.Count == ConfiguracionPartida.CantidadFantasmasIa &&
            !_jugadores.Any(jugador => jugador.IdPersonaje == bots[0].IdPersonaje);
    }

    private static bool EsEstadoInicial(EstadoPartida partida)
    {
        return partida.TiempoSimulacion == TimeSpan.Zero &&
            partida.Laberinto.BolasConsumidas == 0 &&
            partida.Frutas.All(fruta => fruta.Disponible) &&
            partida.Pacmans.All(pacman =>
                pacman.VidasRestantes == ConfiguracionPartida.VidasInicialesPacMan &&
                !pacman.ReaparicionPendiente && pacman.InvencibleHasta == TimeSpan.Zero);
    }

    // Identificadores definidos en el catálogo SQL existente.
    private static bool EsPersonajePacMan(byte? idPersonaje)
    {
        return idPersonaje == 1 || idPersonaje == 2;
    }

    private static bool EsPersonajeFantasma(byte? idPersonaje)
    {
        return idPersonaje == 3 || idPersonaje == 4 || idPersonaje == 5;
    }
}
