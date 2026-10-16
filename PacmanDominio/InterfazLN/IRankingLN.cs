using PacmanDominio.Entidades.Vistas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

// Solo cuentan las partidas multijugador finalizadas; las de contra la IA van al historial.
public interface IRankingLN
{
    // LN valida el rol del catálogo: PACMAN o FANTASMA, sin modificar la vista.
    Task<Respuesta<IEnumerable<RankingEntrada>>> ListarPorRolAsync(string rol);
    Task<Respuesta<RankingEntrada>> ObtenerPosicionAsync(int idUsuario, string rol);

    // Puntos acumulados de la cuenta, sumando ambos roles
    Task<Respuesta<IEnumerable<RankingTotalEntrada>>> ListarTotalAsync();
    Task<Respuesta<RankingTotalEntrada>> ObtenerPosicionTotalAsync(int idUsuario);
}
