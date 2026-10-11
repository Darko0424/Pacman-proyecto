using PacmanDominio.EntidadesTipadas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface IDificultadLN
{
    Task<Respuesta<IEnumerable<TDificultad>>> BuscarAsync(TDificultad datos);
    Task<Respuesta<TDificultad>> ObtenerAsync(TDificultad datos);
    Task<Respuesta<IEnumerable<TDificultad>>> ListarAsync();
}
