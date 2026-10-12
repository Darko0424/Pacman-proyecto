using PacmanDominio.Entidades.Vistas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface IRankingLN
{
    // LN valida el rol del catálogo: PACMAN o FANTASMA, sin modificar la vista.
    Task<Respuesta<IEnumerable<RankingEntrada>>> ListarPorRolAsync(string rol);
    Task<Respuesta<RankingEntrada>> ObtenerPosicionAsync(int idUsuario, string rol);
}
