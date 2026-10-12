using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Items;
using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego;

// La lógica futura aplicará las acciones en orden sobre este estado.
public sealed class EstadoPartida
{
    private readonly List<FrutaEnJuego> _frutas = new();

    public Laberinto Laberinto { get; }
    public IReadOnlyList<PacMan> Pacmans { get; }
    public IReadOnlyList<Fantasma> Fantasmas { get; }
    public TimeSpan TiempoSimulacion { get; private set; }
    public string? CodigoSala { get; private set; }
    public IReadOnlyList<FrutaEnJuego> Frutas { get; }

    // Completar este mapa no determina la victoria final de la serie de niveles.
    public bool MapaCompletado
    {
        get
        {
            return Laberinto.BolasRestantes == 0;
        }
    }

    public bool GanaronFantasmas
    {
        get
        {
            return Pacmans.All(pacman => pacman.EsEspectador);
        }
    }

    public EstadoPartida(
        Laberinto laberinto,
        IEnumerable<PacMan> pacmans,
        IEnumerable<Fantasma> fantasmas,
        IEnumerable<FrutaEnJuego>? frutas = null)
    {
        ArgumentNullException.ThrowIfNull(laberinto);
        ArgumentNullException.ThrowIfNull(pacmans);
        ArgumentNullException.ThrowIfNull(fantasmas);

        Laberinto = laberinto;
        Pacmans = pacmans.ToList().AsReadOnly();
        Fantasmas = fantasmas.ToList().AsReadOnly();

        ValidarComposicion();
        ValidarPosicionesIniciales();

        Frutas = _frutas.AsReadOnly();

        if (frutas != null)
        {
            foreach (var fruta in frutas)
            {
                ValidarFruta(fruta);

                if (_frutas.Any(existente => existente.IdFrutaEnJuego == fruta.IdFrutaEnJuego))
                {
                    throw new ArgumentException("No se puede repetir una instancia de fruta.", nameof(frutas));
                }

                _frutas.Add(fruta);
            }
        }
    }

    public void ActualizarTiempo(TimeSpan tiempoSimulacion)
    {
        if (tiempoSimulacion < TiempoSimulacion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tiempoSimulacion),
                "El tiempo de simulación no puede retroceder.");
        }

        TiempoSimulacion = tiempoSimulacion;
    }

    public bool MoverEntidad(byte idPersonaje, int fila, int columna)
    {
        var entidad = ObtenerEntidad(idPersonaje);

        if (MapaCompletado || GanaronFantasmas)
        {
            return false;
        }

        if (entidad is PacMan pacman && !pacman.PuedeParticipar)
        {
            return false;
        }

        if (!Laberinto.EsTransitable(fila, columna))
        {
            return false;
        }

        // El sistema de movimiento resolverá el trayecto y la velocidad.
        entidad.CambiarPosicion(Laberinto.ObtenerCasilla(fila, columna));

        return true;
    }

    public bool PerderVidaPacMan(byte idPersonaje)
    {
        var pacman = ObtenerPacMan(idPersonaje);

        if (MapaCompletado || GanaronFantasmas)
        {
            return false;
        }

        return pacman.PerderVida(TiempoSimulacion);
    }

    public bool ReaparecerPacMan(byte idPersonaje, int fila, int columna)
    {
        var pacman = ObtenerPacMan(idPersonaje);

        if (MapaCompletado || GanaronFantasmas)
        {
            return false;
        }

        if (!Laberinto.EsPuntoReaparicion(fila, columna))
        {
            return false;
        }

        // La selección segura corresponde a la lógica, no a este modelo.
        return pacman.Reaparecer(
            Laberinto.ObtenerCasilla(fila, columna), TiempoSimulacion);
    }

    public bool ConsumirPunto(byte idPersonaje)
    {
        var pacman = ObtenerPacMan(idPersonaje);

        if (MapaCompletado || GanaronFantasmas || !pacman.PuedeParticipar)
        {
            return false;
        }

        return Laberinto.ConsumirPunto(pacman.Posicion);
    }

    public bool AgregarFruta(FrutaEnJuego fruta)
    {
        ValidarFruta(fruta);

        if (MapaCompletado || GanaronFantasmas ||
            _frutas.Any(existente => existente.IdFrutaEnJuego == fruta.IdFrutaEnJuego))
        {
            return false;
        }

        // LN decide cuándo aparece; el dominio solo valida su incorporación.
        _frutas.Add(fruta);

        return true;
    }

    public bool ConsumirFruta(byte idPersonaje, int idFrutaEnJuego)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idFrutaEnJuego);
        var pacman = ObtenerPacMan(idPersonaje);

        if (MapaCompletado || GanaronFantasmas || !pacman.PuedeParticipar)
        {
            return false;
        }

        var fruta = _frutas.FirstOrDefault(item => item.IdFrutaEnJuego == idFrutaEnJuego);

        if (fruta == null || fruta.Posicion.Fila != pacman.Posicion.Fila ||
            fruta.Posicion.Columna != pacman.Posicion.Columna)
        {
            return false;
        }

        // El consumo no concede poderes ni suma puntos automáticamente.
        return fruta.Consumir(idPersonaje, TiempoSimulacion);
    }

    private void ValidarFruta(FrutaEnJuego fruta)
    {
        ArgumentNullException.ThrowIfNull(fruta);

        if (fruta.Consumida || !Laberinto.EsTransitable(fruta.Posicion.Fila, fruta.Posicion.Columna))
        {
            throw new ArgumentException(
                "La fruta debe estar disponible y en una casilla transitable del laberinto.",
                nameof(fruta));
        }
    }

    // Cada instancia de partida puede asociarse a una sola sala.
    internal bool AsociarSala(string codigoSala)
    {
        if (CodigoSala != null)
        {
            return false;
        }

        CodigoSala = codigoSala;

        return true;
    }

    private Entidad ObtenerEntidad(byte idPersonaje)
    {
        var entidad = Pacmans.Cast<Entidad>()
            .Concat(Fantasmas)
            .FirstOrDefault(actor => actor.IdPersonaje == idPersonaje);

        if (entidad == null)
        {
            throw new ArgumentException(
                "El personaje no participa en esta partida.",
                nameof(idPersonaje));
        }

        return entidad;
    }

    private PacMan ObtenerPacMan(byte idPersonaje)
    {
        if (ObtenerEntidad(idPersonaje) is not PacMan pacman)
        {
            throw new ArgumentException(
                "La acción solo corresponde a un Pac-Man.",
                nameof(idPersonaje));
        }

        return pacman;
    }

    private void ValidarComposicion()
    {
        if (Pacmans.Any(pacman => pacman == null) ||
            Fantasmas.Any(fantasma => fantasma == null))
        {
            throw new ArgumentException("Los participantes no pueden ser nulos.");
        }

        if (Pacmans.Count != ConfiguracionPartida.CantidadPacMan ||
            Fantasmas.Count(fantasma => !fantasma.EsIa) !=
                ConfiguracionPartida.CantidadFantasmasHumanos ||
            Fantasmas.Count(fantasma => fantasma.EsIa) !=
                ConfiguracionPartida.CantidadFantasmasIa)
        {
            throw new ArgumentException(
                "Se requieren dos Pac-Man humanos, dos fantasmas humanos y un fantasma bot.");
        }

        var participantes = Pacmans.Cast<Entidad>().Concat(Fantasmas).ToList();
        var personajesDistintos = participantes
            .Select(participante => participante.IdPersonaje)
            .Distinct()
            .Count();

        if (personajesDistintos != participantes.Count)
        {
            throw new ArgumentException("No se puede repetir un personaje en la partida.");
        }

        var humanos = participantes.Where(participante => !participante.EsIa).ToList();
        var usuariosDistintos = humanos
            .Select(participante => participante.IdUsuario)
            .Distinct()
            .Count();

        if (usuariosDistintos != humanos.Count)
        {
            throw new ArgumentException("Cada humano debe tener un usuario diferente.");
        }
    }

    private void ValidarPosicionesIniciales()
    {
        foreach (var entidad in Pacmans.Cast<Entidad>().Concat(Fantasmas))
        {
            if (!Laberinto.EsTransitable(entidad.Posicion.Fila, entidad.Posicion.Columna))
            {
                throw new ArgumentException(
                    "Todos los actores deben comenzar en casillas transitables del laberinto.");
            }
        }
    }
}
