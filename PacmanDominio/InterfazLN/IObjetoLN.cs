using PacmanDominio.EntidadesTipadas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface IObjetoLN
{
    Task<Respuesta<IEnumerable<TObjeto>>> BuscarAsync(TObjeto datos);
    Task<Respuesta<TObjeto>> ObtenerAsync(TObjeto datos);
    Task<Respuesta<IEnumerable<TObjeto>>> ListarAsync();

    // Objetos que puede sacar de una caja cada bando: PACMAN o FANTASMA
    Task<Respuesta<IEnumerable<TObjeto>>> ListarPorBandoAsync(string bando);
}
