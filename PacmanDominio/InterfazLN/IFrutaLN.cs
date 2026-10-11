using PacmanDominio.EntidadesTipadas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface IFrutaLN
{
    Task<Respuesta<IEnumerable<TFruta>>> BuscarAsync(TFruta datos);
    Task<Respuesta<TFruta>> ObtenerAsync(TFruta datos);
    Task<Respuesta<IEnumerable<TFruta>>> ListarAsync();
}
