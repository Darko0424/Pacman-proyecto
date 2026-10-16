using PacmanDominio.Juego;
using PacmanDominio.Juego.Salas;

namespace PacmanDominio.DTO.TiempoReal;

// Los enums viajan con su valor numérico (EstadoSala, ModoPartida, Bando).
public sealed class SalaDto
{
    public string Codigo { get; init; } = string.Empty;
    public EstadoSala Estado { get; init; }
    public int? IdAnfitrion { get; init; }
    public ModoPartida? Modo { get; init; }
    public int NumeroNivel { get; init; }
    public int CantidadNiveles { get; init; }
    public bool PuedeIniciar { get; init; }
    public int CapacidadHumanos { get; init; }
    public ConfiguracionSalaDto Configuracion { get; init; } = new();
    public IReadOnlyList<JugadorSalaDto> Jugadores { get; init; } = Array.Empty<JugadorSalaDto>();

    public static SalaDto Crear(Sala sala)
    {
        ArgumentNullException.ThrowIfNull(sala);

        var jugadores = sala.Jugadores.Select(jugador => new JugadorSalaDto
        {
            IdUsuario = jugador.IdUsuario,
            NombreUsuario = jugador.NombreUsuario,
            IdPersonaje = jugador.IdPersonaje,
            EstaConectado = jugador.EstaConectado,
            EsAnfitrion = jugador.IdUsuario == sala.IdAnfitrion,
            RolPreferido = jugador.RolPreferido
        }).ToList().AsReadOnly();

        return new SalaDto
        {
            Codigo = sala.Codigo,
            Estado = sala.Estado,
            IdAnfitrion = sala.IdAnfitrion,
            Modo = sala.Modo,
            NumeroNivel = sala.NumeroNivel,
            CantidadNiveles = ConfiguracionPartida.CantidadNiveles,
            PuedeIniciar = sala.PuedeIniciar,
            CapacidadHumanos = ConfiguracionPartida.CantidadMaximaHumanos,
            Configuracion = ConfiguracionSalaDto.Crear(sala.Configuracion),
            Jugadores = jugadores
        };
    }
}

public sealed class JugadorSalaDto
{
    public int IdUsuario { get; init; }
    public string NombreUsuario { get; init; } = string.Empty;
    public byte? IdPersonaje { get; init; }
    public bool EstaConectado { get; init; }
    public bool EsAnfitrion { get; init; }
    public Bando? RolPreferido { get; init; }
}
