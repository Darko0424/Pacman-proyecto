using PacmanDominio.Juego;
using PacmanDominio.Juego.Items;
using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.DTO.TiempoReal;

// Se envía una vez al empezar cada nivel: el mapa no cambia durante el nivel.
// Lo que cambia en cada instante viaja en EstadoPartidaDto.
public sealed class NivelDto
{
    public string? CodigoSala { get; init; }
    public int NumeroNivel { get; init; }
    public ModoPartida Modo { get; init; }
    public int Filas { get; init; }
    public int Columnas { get; init; }

    // Una cadena por fila: '#' muro, ' ' suelo (las bolas van en Puntos)
    public IReadOnlyList<string> Casillas { get; init; } = Array.Empty<string>();

    public IReadOnlyList<CoordenadaDto> PuntosReaparicion { get; init; } = Array.Empty<CoordenadaDto>();
    public IReadOnlyList<CoordenadaDto> PuntosCajas { get; init; } = Array.Empty<CoordenadaDto>();
    public IReadOnlyList<PuntoDto> Puntos { get; init; } = Array.Empty<PuntoDto>();
    public ConfiguracionSalaDto Configuracion { get; init; } = new();

    public static NivelDto Crear(EstadoPartida partida)
    {
        ArgumentNullException.ThrowIfNull(partida);

        var laberinto = partida.Laberinto;
        var filas = Enumerable.Range(0, laberinto.Filas)
            .Select(fila => new string(Enumerable.Range(0, laberinto.Columnas)
                .Select(columna => laberinto.EsTransitable(fila, columna) ? ' ' : '#')
                .ToArray()))
            .ToList()
            .AsReadOnly();

        return new NivelDto
        {
            CodigoSala = partida.CodigoSala,
            NumeroNivel = partida.NumeroNivel,
            Modo = partida.Modo,
            Filas = laberinto.Filas,
            Columnas = laberinto.Columnas,
            Casillas = filas,
            PuntosReaparicion = laberinto.PuntosReaparicion.Select(CoordenadaDto.Crear).ToList().AsReadOnly(),
            PuntosCajas = laberinto.PuntosCajas.Select(CoordenadaDto.Crear).ToList().AsReadOnly(),
            Puntos = laberinto.Puntos.Select(punto => new PuntoDto
            {
                Fila = punto.Posicion.Fila,
                Columna = punto.Posicion.Columna,
                EsDePoder = punto is PuntoPoder
            }).ToList().AsReadOnly(),
            Configuracion = ConfiguracionSalaDto.Crear(partida.Configuracion)
        };
    }
}

public sealed class CoordenadaDto
{
    public int Fila { get; init; }
    public int Columna { get; init; }

    public static CoordenadaDto Crear(Casilla casilla)
    {
        return new CoordenadaDto { Fila = casilla.Fila, Columna = casilla.Columna };
    }
}

public sealed class PuntoDto
{
    public int Fila { get; init; }
    public int Columna { get; init; }
    public bool EsDePoder { get; init; }
}
