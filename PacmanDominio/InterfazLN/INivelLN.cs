using PacmanDominio.EntidadesTipadas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface INivelLN
{
    Task<Respuesta<IEnumerable<TNivel>>> BuscarAsync(TNivel datos);
    Task<Respuesta<TNivel>> ObtenerAsync(TNivel datos);
    Task<Respuesta<IEnumerable<TNivel>>> ListarAsync();
}
