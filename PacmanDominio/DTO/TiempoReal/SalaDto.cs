using PacmanDominio.Juego;
using PacmanDominio.Juego.Salas;

namespace PacmanDominio.DTO.TiempoReal;

public sealed class SalaDto
{
    public string Codigo { get; init; } = string.Empty;
    public bool PartidaIniciada { get; init; }
    public bool PuedeIniciar { get; init; }
    public int CapacidadHumanos { get; init; }
    public byte? IdPersonajeBot { get; init; }
    public IReadOnlyList<JugadorSalaDto> Jugadores { get; init; } = Array.Empty<JugadorSalaDto>();

    public static SalaDto Crear(Sala sala)
    {
        ArgumentNullException.ThrowIfNull(sala);

        var jugadores = sala.Jugadores.Select(jugador => new JugadorSalaDto
        {
            IdUsuario = jugador.IdUsuario,
            IdPersonaje = jugador.IdPersonaje,
            EstaConectado = jugador.EstaConectado
        }).ToList().AsReadOnly();

        return new SalaDto
        {
            Codigo = sala.Codigo,
            PartidaIniciada = sala.PartidaIniciada,
            PuedeIniciar = sala.PuedeIniciar,
            CapacidadHumanos = ConfiguracionPartida.CantidadPacMan +
                ConfiguracionPartida.CantidadFantasmasHumanos,
            IdPersonajeBot = sala.IdPersonajeBot,
            Jugadores = jugadores
        };
    }
}

public sealed class JugadorSalaDto
{
    public int IdUsuario { get; init; }
    public byte? IdPersonaje { get; init; }
    public bool EstaConectado { get; init; }
}
