using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Items;
using PacmanDominio.Juego.Salas;

namespace PacmanDominio.DTO.TiempoReal;

// Lo que cambia durante el nivel. El mapa se envía aparte en NivelDto.
public sealed class EstadoPartidaDto
{
    public string? CodigoSala { get; init; }
    public int NumeroNivel { get; init; }

    // Todos los tiempos del transporte son segundos desde el inicio del nivel.
    public double TiempoSimulacionSegundos { get; init; }

    public bool Terminada { get; init; }
    public Bando? BandoGanador { get; init; }
    public int BolasRestantes { get; init; }

    // Índices fila * columnas + columna de las bolas ya comidas
    public IReadOnlyList<int> PuntosConsumidos { get; init; } = Array.Empty<int>();

    public IReadOnlyList<PacManDto> Pacmans { get; init; } = Array.Empty<PacManDto>();
    public IReadOnlyList<FantasmaDto> Fantasmas { get; init; } = Array.Empty<FantasmaDto>();

    // Solo las cajas que siguen en el mapa
    public IReadOnlyList<CajaDto> Cajas { get; init; } = Array.Empty<CajaDto>();

    // Trampas de hielo puestas por los fantasmas que nadie ha pisado
    public IReadOnlyList<TrampaDto> Trampas { get; init; } = Array.Empty<TrampaDto>();

    public IReadOnlyList<EfectoDto> Efectos { get; init; } = Array.Empty<EfectoDto>();

    // Lo que pasó desde el estado anterior (sonidos y animaciones)
    public IReadOnlyList<EventoDto> Eventos { get; init; } = Array.Empty<EventoDto>();

    // La sala es opcional para simulaciones locales. Si se incluye, debe poseer este estado.
    // El gestor toma la instantánea dentro de la misma secuencia de acciones de la partida.
    public static EstadoPartidaDto Crear(
        EstadoPartida partida, Sala? sala = null, IEnumerable<EventoJuego>? eventos = null)
    {
        ArgumentNullException.ThrowIfNull(partida);

        if (sala != null && !ReferenceEquals(sala.Partida, partida))
        {
            throw new ArgumentException("La partida no pertenece a la sala indicada.", nameof(sala));
        }

        var tiempo = partida.TiempoSimulacion;
        var columnas = partida.Laberinto.Columnas;

        return new EstadoPartidaDto
        {
            CodigoSala = partida.CodigoSala,
            NumeroNivel = partida.NumeroNivel,
            TiempoSimulacionSegundos = tiempo.TotalSeconds,
            Terminada = partida.Terminada,
            BandoGanador = partida.BandoGanador,
            BolasRestantes = partida.Laberinto.BolasRestantes,
            PuntosConsumidos = partida.Laberinto.Puntos
                .Where(punto => punto.Consumido)
                .Select(punto => punto.Posicion.Fila * columnas + punto.Posicion.Columna)
                .ToList()
                .AsReadOnly(),
            Pacmans = partida.Pacmans
                .Select(pacman => new PacManDto
                {
                    IdPersonaje = pacman.IdPersonaje,
                    IdUsuario = pacman.IdUsuario,
                    NombreUsuario = ObtenerNombre(pacman, sala),
                    EsIa = pacman.EsIa,
                    ControlIa = pacman.ControlIa,
                    Fila = pacman.Posicion.Fila,
                    Columna = pacman.Posicion.Columna,
                    Direccion = pacman.Direccion,
                    Avance = pacman.Avance,
                    VidasRestantes = pacman.VidasRestantes,
                    Eliminado = pacman.Eliminado,
                    ReaparicionPendiente = pacman.ReaparicionPendiente,
                    EsInvencible = pacman.EsInvencible(tiempo),
                    Congelado = pacman.EstaCongelado(tiempo),
                    IdObjetoGuardado = pacman.IdObjetoGuardado,
                    Puntuacion = pacman.Puntuacion
                })
                .ToList()
                .AsReadOnly(),
            Fantasmas = partida.Fantasmas
                .Select(fantasma => new FantasmaDto
                {
                    IdPersonaje = fantasma.IdPersonaje,
                    IdUsuario = fantasma.IdUsuario,
                    NombreUsuario = ObtenerNombre(fantasma, sala),
                    EsIa = fantasma.EsIa,
                    ControlIa = fantasma.ControlIa,
                    Fila = fantasma.Posicion.Fila,
                    Columna = fantasma.Posicion.Columna,
                    Direccion = fantasma.Direccion,
                    Avance = fantasma.Avance,
                    VidasRestantes = fantasma.VidasRestantes,
                    Eliminado = fantasma.Eliminado,
                    ReaparicionPendiente = fantasma.ReaparicionPendiente,
                    EsInvencible = fantasma.EsInvencible(tiempo),
                    Congelado = fantasma.EstaCongelado(tiempo),
                    IdObjetoGuardado = fantasma.IdObjetoGuardado,
                    Puntuacion = fantasma.Puntuacion,
                    Asustado = fantasma.EstaAsustado(tiempo),
                    Aturdido = fantasma.EstaAturdido(tiempo)
                })
                .ToList()
                .AsReadOnly(),
            Cajas = partida.Cajas
                .Where(caja => caja.Disponible)
                .Select(caja => new CajaDto
                {
                    IdCaja = caja.IdCaja,
                    Fila = caja.Posicion.Fila,
                    Columna = caja.Posicion.Columna
                })
                .ToList()
                .AsReadOnly(),
            Trampas = partida.Trampas
                .Select(trampa => new TrampaDto
                {
                    IdTrampa = trampa.IdTrampa,
                    Fila = trampa.Posicion.Fila,
                    Columna = trampa.Posicion.Columna,
                    IdPersonajeOrigen = trampa.IdPersonajeOrigen,
                    FinSegundos = trampa.Fin.TotalSeconds
                })
                .ToList()
                .AsReadOnly(),
            Efectos = partida.EfectosActivos
                .Select(efecto => new EfectoDto
                {
                    IdEfecto = efecto.IdEfecto,
                    IdObjeto = efecto.IdObjeto,
                    TipoEfecto = efecto.TipoEfecto,
                    IdPersonajeOrigen = efecto.IdPersonajeOrigen,
                    BandoOrigen = efecto.BandoOrigen,
                    FinSegundos = efecto.Fin.TotalSeconds
                })
                .ToList()
                .AsReadOnly(),
            Eventos = (eventos ?? Enumerable.Empty<EventoJuego>())
                .Select(evento => new EventoDto
                {
                    Tipo = evento.Tipo,
                    IdPersonaje = evento.IdPersonaje,
                    IdOtroPersonaje = evento.IdOtroPersonaje,
                    Fila = evento.Fila,
                    Columna = evento.Columna,
                    IdObjeto = evento.IdObjeto
                })
                .ToList()
                .AsReadOnly()
        };
    }

    // null para la IA o cuando no se proporcionó la sala
    private static string? ObtenerNombre(Entidad actor, Sala? sala)
    {
        if (sala == null || actor.EsIa)
        {
            return null;
        }

        return sala.Jugadores.FirstOrDefault(jugador => jugador.IdUsuario == actor.IdUsuario)?.NombreUsuario;
    }
}

public sealed class PacManDto
{
    public byte IdPersonaje { get; init; }
    public int? IdUsuario { get; init; }
    public string? NombreUsuario { get; init; }
    public bool EsIa { get; init; }
    public bool ControlIa { get; init; }
    public int Fila { get; init; }
    public int Columna { get; init; }
    public Direccion Direccion { get; init; }
    public double Avance { get; init; }
    public int VidasRestantes { get; init; }
    public bool Eliminado { get; init; }
    public bool ReaparicionPendiente { get; init; }
    public bool EsInvencible { get; init; }

    // Pisó una trampa de hielo (el objeto Hielo se ve en Efectos)
    public bool Congelado { get; init; }
    public byte? IdObjetoGuardado { get; init; }
    public int Puntuacion { get; init; }
}

public sealed class FantasmaDto
{
    public byte IdPersonaje { get; init; }
    public int? IdUsuario { get; init; }
    public string? NombreUsuario { get; init; }
    public bool EsIa { get; init; }
    public bool ControlIa { get; init; }
    public int Fila { get; init; }
    public int Columna { get; init; }
    public Direccion Direccion { get; init; }
    public double Avance { get; init; }
    public int VidasRestantes { get; init; }
    public bool Eliminado { get; init; }
    public bool ReaparicionPendiente { get; init; }
    public bool EsInvencible { get; init; }

    // Pisó una trampa de hielo (el objeto Hielo se ve en Efectos)
    public bool Congelado { get; init; }
    public byte? IdObjetoGuardado { get; init; }
    public int Puntuacion { get; init; }
    public bool Asustado { get; init; }
    public bool Aturdido { get; init; }
}

public sealed class CajaDto
{
    public int IdCaja { get; init; }
    public int Fila { get; init; }
    public int Columna { get; init; }
}

public sealed class TrampaDto
{
    public int IdTrampa { get; init; }
    public int Fila { get; init; }
    public int Columna { get; init; }
    public byte IdPersonajeOrigen { get; init; }
    public double FinSegundos { get; init; }
}

public sealed class EfectoDto
{
    public int IdEfecto { get; init; }
    public byte IdObjeto { get; init; }
    public string TipoEfecto { get; init; } = string.Empty;
    public byte IdPersonajeOrigen { get; init; }
    public Bando BandoOrigen { get; init; }
    public double FinSegundos { get; init; }
}

public sealed class EventoDto
{
    public TipoEventoJuego Tipo { get; init; }
    public byte? IdPersonaje { get; init; }
    public byte? IdOtroPersonaje { get; init; }
    public int? Fila { get; init; }
    public int? Columna { get; init; }
    public byte? IdObjeto { get; init; }
}
