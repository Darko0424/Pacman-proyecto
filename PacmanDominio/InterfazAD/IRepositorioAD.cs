using PacmanUtilitarios;
using System.Linq.Expressions;

namespace PacmanDominio.InterfazAD
{
    public interface IRepositorioAD<TEntity> where TEntity : class
    {
        Task<Respuesta<TEntity>> InsertarAsync(TEntity objEntidad);

        Task<Respuesta<TEntity>> ModificarAsync(TEntity objEntidad);

        Task<Respuesta<bool>> EliminarAsync(TEntity objEntidad);

        Task<Respuesta<IEnumerable<TEntity>>> ListarAsync(List<string>? objIncludes = null);

        Task<Respuesta<IEnumerable<TEntity>>> BuscarAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null);

        Task<Respuesta<TEntity>> ObtenerEntidadAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null);

        Task<Respuesta<int?>> ContarAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null);
    }
}
