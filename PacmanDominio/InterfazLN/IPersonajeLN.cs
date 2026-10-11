using PacmanDominio.EntidadesTipadas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface IPersonajeLN
{
    Task<Respuesta<IEnumerable<TPersonaje>>> BuscarAsync(TPersonaje datos);
    Task<Respuesta<TPersonaje>> ObtenerAsync(TPersonaje datos);
    Task<Respuesta<IEnumerable<TPersonaje>>> ListarAsync();
}
