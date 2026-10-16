using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Items;
using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego;

// Estado de un nivel en juego. El motor aplica las acciones en orden sobre este
// estado; aquí solo se validan las operaciones, no las reglas del juego.
public sealed class EstadoPartida
{
    private readonly List<CajaObjeto> _cajas = new();
    private readonly List<EfectoActivo> _efectos = new();

    public int NumeroNivel { get; }
    public ModoPartida Modo { get; }
    public ConfiguracionSala Configuracion { get; }
    public Laberinto Laberinto { get; }
    public IReadOnlyList<PacMan> Pacmans { get; }
    public IReadOnlyList<Fantasma> Fantasmas { get; }
    public IReadOnlyList<CajaObjeto> Cajas { get; }
    public IReadOnlyList<EfectoActivo> EfectosActivos { get; }
    public TimeSpan TiempoSimulacion { get; private set; }
    public string? CodigoSala { get; private set; }

    // Puntos extra del bando ganador; se otorgan una sola vez al terminar el nivel
    public bool BonificacionOtorgada { get; private set; }

    public IEnumerable<Entidad> Actores
    {
        get
        {
            return Pacmans.Cast<Entidad>().Concat(Fantasmas);
        }
    }

    public bool MapaCompletado
    {
        get
        {
            return Laberinto.BolasRestantes == 0;
        }
    }

    // Ganan los Pac-Man si se comen todas las bolas o si eliminan a todos los
    // fantasmas; ganan los fantasmas si eliminan a todos los Pac-Man.
    public Bando? BandoGanador
    {
        get
        {
            if (MapaCompletado || Fantasmas.All(fantasma => fantasma.Eliminado))
            {
                return Bando.PacMan;
            }

            if (Pacmans.All(pacman => pacman.Eliminado))
            {
                return Bando.Fantasma;
            }

            return null;
        }
    }

    public bool Terminada
    {
        get
        {
            return BandoGanador.HasValue;
        }
    }

    // Recién creado: nadie se ha movido, comido ni usado nada
    internal bool EsEstadoInicial
    {
        get
        {
            return TiempoSimulacion == TimeSpan.Zero &&
                Laberinto.BolasConsumidas == 0 &&
                _cajas.Count == 0 &&
                _efectos.Count == 0 &&
                Actores.All(actor =>
                    actor.VidasRestantes == Configuracion.Vidas &&
                    !actor.ReaparicionPendiente &&
                    actor.InvencibleHasta == TimeSpan.Zero &&
                    !actor.IdObjetoGuardado.HasValue &&
                    actor.Puntuacion == 0);
        }
    }

    public EstadoPartida(
        int numeroNivel,
        ModoPartida modo,
        ConfiguracionSala configuracion,
        Laberinto laberinto,
        IEnumerable<PacMan> pacmans,
        IEnumerable<Fantasma> fantasmas)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        ArgumentNullException.ThrowIfNull(laberinto);
        ArgumentNullException.ThrowIfNull(pacmans);
        ArgumentNullException.ThrowIfNull(fantasmas);

        if (numeroNivel < 1 || numeroNivel > ConfiguracionPartida.CantidadNiveles)
        {
            throw new ArgumentOutOfRangeException(nameof(numeroNivel));
        }

        if (!Enum.IsDefined(modo))
        {
            throw new ArgumentOutOfRangeException(nameof(modo));
        }

        NumeroNivel = numeroNivel;
        Modo = modo;
        Configuracion = configuracion;
        Laberinto = laberinto;
        Pacmans = pacmans.ToList().AsReadOnly();
        Fantasmas = fantasmas.ToList().AsReadOnly();
        Cajas = _cajas.AsReadOnly();
        EfectosActivos = _efectos.AsReadOnly();

        ValidarComposicion();
        ValidarActoresIniciales();
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

    public Entidad ObtenerActor(byte idPersonaje)
    {
        var actor = Actores.FirstOrDefault(entidad => entidad.IdPersonaje == idPersonaje);

        if (actor == null)
        {
            throw new ArgumentException(
                "El personaje no participa en esta partida.",
                nameof(idPersonaje));
        }

        return actor;
    }

    public Entidad? ObtenerActorDeUsuario(int idUsuario)
    {
        return Actores.FirstOrDefault(entidad => entidad.IdUsuario == idUsuario);
    }

    public bool MoverEntidad(byte idPersonaje, int fila, int columna)
    {
        var actor = ObtenerActor(idPersonaje);

        if (Terminada || !actor.PuedeParticipar)
        {
            return false;
        }

        if (actor is Fantasma fantasma && fantasma.EstaAturdido(TiempoSimulacion))
        {
            return false;
        }

        if (!Laberinto.EsTransitable(fila, columna))
        {
            return false;
        }

        // El sistema de movimiento resolverá el trayecto y la velocidad.
        actor.CambiarPosicion(Laberinto.ObtenerCasilla(fila, columna));

        return true;
    }

    // Dirección en la que avanza y fracción recorrida hacia la siguiente casilla.
    // La dirección debe llevar a una casilla transitable (o ser Ninguna).
    public bool ActualizarDesplazamiento(byte idPersonaje, Direccion direccion, double avance)
    {
        var actor = ObtenerActor(idPersonaje);

        if (!Enum.IsDefined(direccion) || double.IsNaN(avance) || avance < 0 || avance >= 1)
        {
            return false;
        }

        if (direccion == Direccion.Ninguna)
        {
            avance = 0;
        }
        else
        {
            var (fila, columna) = Desplazar(actor.Posicion, direccion);

            if (!Laberinto.EsTransitable(fila, columna))
            {
                return false;
            }
        }

        actor.ActualizarDesplazamiento(direccion, avance);

        return true;
    }

    // Una vez terminado el nivel: suma los puntos a todos los del bando ganador
    public bool OtorgarBonificacion(int puntos)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(puntos);

        var ganador = BandoGanador;

        if (!ganador.HasValue || BonificacionOtorgada)
        {
            return false;
        }

        foreach (var actor in Actores.Where(actor => actor.Bando == ganador))
        {
            actor.SumarPuntos(puntos);
        }

        BonificacionOtorgada = true;

        return true;
    }

    public static (int Fila, int Columna) Desplazar(Casilla origen, Direccion direccion)
    {
        ArgumentNullException.ThrowIfNull(origen);

        return direccion switch
        {
            Direccion.Arriba => (origen.Fila - 1, origen.Columna),
            Direccion.Abajo => (origen.Fila + 1, origen.Columna),
            Direccion.Izquierda => (origen.Fila, origen.Columna - 1),
            Direccion.Derecha => (origen.Fila, origen.Columna + 1),
            _ => (origen.Fila, origen.Columna)
        };
    }

    // Pac-Man atrapado o fantasma comido: pierde una vida y queda pendiente de reaparecer
    public bool PerderVida(byte idPersonaje)
    {
        var actor = ObtenerActor(idPersonaje);

        if (Terminada || !actor.PerderVida(TiempoSimulacion))
        {
            return false;
        }

        if (actor is Fantasma fantasma)
        {
            fantasma.Recuperar();
        }

        return true;
    }

    public bool Reaparecer(byte idPersonaje, int fila, int columna)
    {
        var actor = ObtenerActor(idPersonaje);

        if (Terminada || !Laberinto.EsPuntoReaparicion(fila, columna))
        {
            return false;
        }

        // La selección de un punto seguro corresponde a la lógica, no a este modelo.
        return actor.Reaparecer(Laberinto.ObtenerCasilla(fila, columna), TiempoSimulacion);
    }

    // Devuelve la bola que se comió el Pac-Man en su casilla (PuntoPoder si era de poder)
    public Punto? ConsumirPunto(byte idPersonaje)
    {
        var pacman = ObtenerPacMan(idPersonaje);

        if (Terminada || !pacman.PuedeParticipar)
        {
            return null;
        }

        return Laberinto.ConsumirPunto(pacman.Posicion);
    }

    public bool SumarPuntos(byte idPersonaje, int puntos)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(puntos);
        var actor = ObtenerActor(idPersonaje);

        if (Terminada)
        {
            return false;
        }

        actor.SumarPuntos(puntos);

        return true;
    }

    public bool RegistrarFantasmaComido(byte idPacMan)
    {
        var pacman = ObtenerPacMan(idPacMan);

        if (Terminada)
        {
            return false;
        }

        pacman.RegistrarFantasmaComido();

        return true;
    }

    public bool RegistrarPacManAtrapado(byte idFantasma)
    {
        var fantasma = ObtenerFantasma(idFantasma);

        if (Terminada)
        {
            return false;
        }

        fantasma.RegistrarPacManAtrapado();

        return true;
    }

    public bool AsustarFantasma(byte idFantasma, TimeSpan hasta)
    {
        var fantasma = ObtenerFantasma(idFantasma);

        if (Terminada || !fantasma.PuedeParticipar || hasta <= TiempoSimulacion)
        {
            return false;
        }

        fantasma.Asustar(hasta);

        return true;
    }

    public bool AturdirFantasma(byte idFantasma, TimeSpan hasta)
    {
        var fantasma = ObtenerFantasma(idFantasma);

        if (Terminada || !fantasma.PuedeParticipar || hasta <= TiempoSimulacion)
        {
            return false;
        }

        fantasma.Aturdir(hasta);

        return true;
    }

    // La caja solo puede aparecer en un punto de cajas libre
    public bool AparecerCaja(CajaObjeto caja)
    {
        ArgumentNullException.ThrowIfNull(caja);

        if (Terminada || caja.Recogida ||
            !Laberinto.EsPuntoCaja(caja.Posicion.Fila, caja.Posicion.Columna) ||
            _cajas.Any(existente => existente.IdCaja == caja.IdCaja) ||
            _cajas.Any(existente => existente.Disponible &&
                existente.Posicion.Fila == caja.Posicion.Fila &&
                existente.Posicion.Columna == caja.Posicion.Columna))
        {
            return false;
        }

        _cajas.Add(caja);

        return true;
    }

    // El objeto lo sortea la lógica entre los del bando de quien recoge la caja.
    // Con el espacio ocupado la caja se queda en el mapa.
    public bool RecogerCaja(byte idPersonaje, int idCaja, byte idObjeto)
    {
        ArgumentOutOfRangeException.ThrowIfZero(idObjeto);
        var actor = ObtenerActor(idPersonaje);
        var caja = _cajas.FirstOrDefault(item => item.IdCaja == idCaja);

        if (Terminada || caja == null || !caja.Disponible || !actor.PuedeParticipar ||
            actor.IdObjetoGuardado.HasValue ||
            caja.Posicion.Fila != actor.Posicion.Fila ||
            caja.Posicion.Columna != actor.Posicion.Columna)
        {
            return false;
        }

        caja.Recoger(idPersonaje);

        return actor.GuardarObjeto(idObjeto);
    }

    // Saca el objeto guardado; la lógica crea el EfectoActivo correspondiente
    public byte? UsarObjeto(byte idPersonaje)
    {
        var actor = ObtenerActor(idPersonaje);

        if (Terminada || !actor.PuedeParticipar)
        {
            return null;
        }

        return actor.UsarObjeto();
    }

    public bool AgregarEfecto(EfectoActivo efecto)
    {
        ArgumentNullException.ThrowIfNull(efecto);
        ObtenerActor(efecto.IdPersonajeOrigen);

        if (Terminada || _efectos.Any(existente => existente.IdEfecto == efecto.IdEfecto))
        {
            return false;
        }

        _efectos.Add(efecto);

        return true;
    }

    public bool RetirarEfecto(int idEfecto)
    {
        return _efectos.RemoveAll(efecto => efecto.IdEfecto == idEfecto) > 0;
    }

    // El jugador se desconectó (abandono = false) o se fue (abandono = true)
    public bool CederControlAIa(int idUsuario, bool abandono)
    {
        var actor = ObtenerActorDeUsuario(idUsuario);

        return actor != null && actor.CederControlAIa(abandono);
    }

    public bool RecuperarControl(int idUsuario)
    {
        var actor = ObtenerActorDeUsuario(idUsuario);

        return actor != null && actor.RecuperarControl();
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

    private PacMan ObtenerPacMan(byte idPersonaje)
    {
        if (ObtenerActor(idPersonaje) is not PacMan pacman)
        {
            throw new ArgumentException(
                "La acción solo corresponde a un Pac-Man.",
                nameof(idPersonaje));
        }

        return pacman;
    }

    private Fantasma ObtenerFantasma(byte idPersonaje)
    {
        if (ObtenerActor(idPersonaje) is not Fantasma fantasma)
        {
            throw new ArgumentException(
                "La acción solo corresponde a un fantasma.",
                nameof(idPersonaje));
        }

        return fantasma;
    }

    private void ValidarComposicion()
    {
        if (Pacmans.Any(pacman => pacman == null) ||
            Fantasmas.Any(fantasma => fantasma == null))
        {
            throw new ArgumentException("Los participantes no pueden ser nulos.");
        }

        var participantes = Actores.ToList();
        var humanos = participantes.Where(participante => !participante.EsIa).ToList();

        if (participantes.Select(participante => participante.IdPersonaje).Distinct().Count() !=
            participantes.Count)
        {
            throw new ArgumentException("No se puede repetir un personaje en la partida.");
        }

        if (humanos.Select(participante => participante.IdUsuario).Distinct().Count() !=
            humanos.Count)
        {
            throw new ArgumentException("Cada humano debe tener un usuario diferente.");
        }

        if (Modo == ModoPartida.ContraIa)
        {
            if (Pacmans.Count != ConfiguracionPartida.CantidadPacManContraIa ||
                Fantasmas.Count != ConfiguracionPartida.CantidadFantasmas ||
                humanos.Count != 1)
            {
                throw new ArgumentException(
                    "Contra la IA se juega con 1 Pac-Man, 3 fantasmas y un solo humano.");
            }

            return;
        }

        // Un multijugador puede quedar con un solo humano si los demás se fueron
        if (Pacmans.Count != ConfiguracionPartida.CantidadPacMan ||
            Fantasmas.Count != ConfiguracionPartida.CantidadFantasmas ||
            humanos.Count < 1 ||
            humanos.Count > ConfiguracionPartida.CantidadMaximaHumanos ||
            Fantasmas.Count(fantasma => !fantasma.EsIa) >
                ConfiguracionPartida.CantidadMaximaFantasmasHumanos)
        {
            throw new ArgumentException(
                "El multijugador tiene 2 Pac-Man y 3 fantasmas, con 1 a 4 humanos " +
                "y como máximo 2 fantasmas humanos.");
        }
    }

    private void ValidarActoresIniciales()
    {
        foreach (var actor in Actores)
        {
            if (!Laberinto.EsTransitable(actor.Posicion.Fila, actor.Posicion.Columna))
            {
                throw new ArgumentException(
                    "Todos los actores deben comenzar en casillas transitables del laberinto.");
            }

            if (actor.VidasRestantes != Configuracion.Vidas)
            {
                throw new ArgumentException(
                    "Todos los actores deben comenzar con las vidas de la configuración.");
            }
        }
    }
}
