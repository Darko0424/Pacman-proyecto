using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Items;
using PacmanDominio.Juego.Mapa;
using PacmanDominio.Juego.Salas;

namespace PacmanDominio.DTO.TiempoReal;

public sealed class EstadoPartidaDto
{
    public string? CodigoSala { get; init; }
    // Todos los tiempos del transporte son segundos desde el inicio de la simulación.
    public double TiempoSimulacionSegundos { get; init; }
    public bool MapaCompletado { get; init; }
    public bool GanaronFantasmas { get; init; }
    public int Filas { get; init; }
    public int Columnas { get; init; }
    public int BolasRestantes { get; init; }
    public int BolasConsumidas { get; init; }
    public IReadOnlyList<CasillaDto> Casillas { get; init; } = Array.Empty<CasillaDto>();
    public IReadOnlyList<CasillaDto> PuntosReaparicion { get; init; } = Array.Empty<CasillaDto>();
    public IReadOnlyList<PuntoDto> Puntos { get; init; } = Array.Empty<PuntoDto>();
    public IReadOnlyList<PacManDto> Pacmans { get; init; } = Array.Empty<PacManDto>();
    public IReadOnlyList<FantasmaDto> Fantasmas { get; init; } = Array.Empty<FantasmaDto>();
    public IReadOnlyList<FrutaDto> Frutas { get; init; } = Array.Empty<FrutaDto>();

    // La sala es opcional para simulaciones locales. Si se incluye, debe poseer este estado.
    // El gestor toma la instantánea dentro de la misma secuencia de acciones de la partida.
    public static EstadoPartidaDto Crear(EstadoPartida partida, Sala? sala = null)
    {
        ArgumentNullException.ThrowIfNull(partida);

        if (sala != null && !ReferenceEquals(sala.Partida, partida))
        {
            throw new ArgumentException("La partida no pertenece a la sala indicada.", nameof(sala));
        }

        return new EstadoPartidaDto
        {
            CodigoSala = partida.CodigoSala,
            TiempoSimulacionSegundos = partida.TiempoSimulacion.TotalSeconds,
            MapaCompletado = partida.MapaCompletado,
            GanaronFantasmas = partida.GanaronFantasmas,
            Filas = partida.Laberinto.Filas,
            Columnas = partida.Laberinto.Columnas,
            BolasRestantes = partida.Laberinto.BolasRestantes,
            BolasConsumidas = partida.Laberinto.BolasConsumidas,
            Casillas = partida.Laberinto.Casillas.Select(CopiarCasilla).ToList().AsReadOnly(),
            PuntosReaparicion = partida.Laberinto.PuntosReaparicion
                .Select(CopiarCasilla).ToList().AsReadOnly(),
            Puntos = partida.Laberinto.Puntos.Select(CopiarPunto).ToList().AsReadOnly(),
            Pacmans = partida.Pacmans.Select(pacman =>
                CopiarPacMan(pacman, partida.TiempoSimulacion, sala)).ToList().AsReadOnly(),
            Fantasmas = partida.Fantasmas.Select(fantasma =>
                CopiarFantasma(fantasma, sala)).ToList().AsReadOnly(),
            Frutas = partida.Frutas.Select(CopiarFruta).ToList().AsReadOnly()
        };
    }

    private static CasillaDto CopiarCasilla(Casilla casilla)
    {
        return new CasillaDto
        {
            Fila = casilla.Fila,
            Columna = casilla.Columna,
            EsTransitable = casilla.EsTransitable
        };
    }

    private static PuntoDto CopiarPunto(Punto punto)
    {
        return new PuntoDto
        {
            Fila = punto.Posicion.Fila,
            Columna = punto.Posicion.Columna,
            EsDePoder = punto is PuntoPoder,
            Consumido = punto.Consumido
        };
    }

    private static PacManDto CopiarPacMan(PacMan pacman, TimeSpan tiempoSimulacion, Sala? sala)
    {
        return new PacManDto
        {
            IdPersonaje = pacman.IdPersonaje,
            IdUsuario = pacman.IdUsuario!.Value,
            Fila = pacman.Posicion.Fila,
            Columna = pacman.Posicion.Columna,
            VidasRestantes = pacman.VidasRestantes,
            EsEspectador = pacman.EsEspectador,
            ReaparicionPendiente = pacman.ReaparicionPendiente,
            EsInvencible = pacman.EsInvencible(tiempoSimulacion),
            InvencibleHastaSegundos = pacman.InvencibleHasta.TotalSeconds,
            EstaConectado = ObtenerConexion(pacman.IdUsuario, sala)
        };
    }

    private static FantasmaDto CopiarFantasma(Fantasma fantasma, Sala? sala)
    {
        return new FantasmaDto
        {
            IdPersonaje = fantasma.IdPersonaje,
            IdUsuario = fantasma.IdUsuario,
            EsIa = fantasma.EsIa,
            Fila = fantasma.Posicion.Fila,
            Columna = fantasma.Posicion.Columna,
            EstaConectado = ObtenerConexion(fantasma.IdUsuario, sala)
        };
    }

    private static FrutaDto CopiarFruta(FrutaEnJuego fruta)
    {
        return new FrutaDto
        {
            IdFrutaEnJuego = fruta.IdFrutaEnJuego,
            IdFruta = fruta.IdFruta,
            Nombre = fruta.Nombre,
            Efecto = fruta.Efecto,
            DuracionSeg = fruta.DuracionSeg,
            Puntos = fruta.Puntos,
            Fila = fruta.Posicion.Fila,
            Columna = fruta.Posicion.Columna,
            Consumida = fruta.Consumida,
            Disponible = fruta.Disponible,
            IdPersonajeConsumidor = fruta.IdPersonajeConsumidor,
            TiempoConsumoSegundos = fruta.TiempoConsumo?.TotalSeconds
        };
    }

    private static bool? ObtenerConexion(int? idUsuario, Sala? sala)
    {
        if (sala == null || !idUsuario.HasValue)
        {
            return null;
        }

        return sala.Jugadores.Single(jugador => jugador.IdUsuario == idUsuario).EstaConectado;
    }
}

public sealed class CasillaDto
{
    public int Fila { get; init; }
    public int Columna { get; init; }
    public bool EsTransitable { get; init; }
}

public sealed class PuntoDto
{
    public int Fila { get; init; }
    public int Columna { get; init; }
    public bool EsDePoder { get; init; }
    public bool Consumido { get; init; }
}

public sealed class PacManDto
{
    public byte IdPersonaje { get; init; }
    public int IdUsuario { get; init; }
    public int Fila { get; init; }
    public int Columna { get; init; }
    public int VidasRestantes { get; init; }
    public bool EsEspectador { get; init; }
    public bool ReaparicionPendiente { get; init; }
    public bool EsInvencible { get; init; }
    public double InvencibleHastaSegundos { get; init; }
    // null indica que no se proporcionó información de la sala.
    public bool? EstaConectado { get; init; }
}

public sealed class FantasmaDto
{
    public byte IdPersonaje { get; init; }
    public int? IdUsuario { get; init; }
    public bool EsIa { get; init; }
    public int Fila { get; init; }
    public int Columna { get; init; }
    // null para el bot o cuando no se proporcionó información de la sala.
    public bool? EstaConectado { get; init; }
}

public sealed class FrutaDto
{
    public int IdFrutaEnJuego { get; init; }
    public byte IdFruta { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Efecto { get; init; } = string.Empty;
    public byte DuracionSeg { get; init; }
    public int Puntos { get; init; }
    public int Fila { get; init; }
    public int Columna { get; init; }
    public bool Consumida { get; init; }
    public bool Disponible { get; init; }
    public byte? IdPersonajeConsumidor { get; init; }
    public double? TiempoConsumoSegundos { get; init; }
}
