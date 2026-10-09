using Microsoft.EntityFrameworkCore;
using PacmanDominio.InterfazAD;
using PacmanUtilitarios;
using System.Linq.Expressions;

namespace PacmanAccesoDatos.Implementaciones;

public class RepositorioAD<TEntity> : IRepositorioAD<TEntity>
    where TEntity : class
{
    protected readonly DbContext _context;

    public RepositorioAD(DbContext context)
    {
        _context = context;
    }

    public async Task<Respuesta<TEntity>> InsertarAsync(TEntity objEntidad)
    {
        Respuesta<TEntity> objRespuesta = new();

        try
        {
            await _context.Set<TEntity>().AddAsync(objEntidad);
            await _context.SaveChangesAsync();
            objRespuesta.Data = objEntidad;
        }
        catch (Exception ex)
        {
            objRespuesta.Success = false;
            objRespuesta.Error = ex.Message;
            objRespuesta.Data = null;
        }

        return objRespuesta;
    }

    public async Task<Respuesta<TEntity>> ModificarAsync(TEntity objEntidad)
    {
        Respuesta<TEntity> objRespuesta = new();

        try
        {
            _context.Set<TEntity>().Update(objEntidad);
            await _context.SaveChangesAsync();
            objRespuesta.Data = objEntidad;
        }
        catch (Exception ex)
        {
            objRespuesta.Success = false;
            objRespuesta.Error = ex.Message;
            objRespuesta.Data = null;
        }

        return objRespuesta;
    }

    public async Task<Respuesta<bool>> EliminarAsync(TEntity objEntidad)
    {
        Respuesta<bool> objRespuesta = new();

        try
        {
            _context.Entry(objEntidad).State = EntityState.Deleted;
            await _context.SaveChangesAsync();
            objRespuesta.Data = true;
        }
        catch (Exception ex)
        {
            objRespuesta.Success = false;
            objRespuesta.Error = ex.Message;
            objRespuesta.Data = false;
        }

        return objRespuesta;
    }

    public async Task<Respuesta<IEnumerable<TEntity>>> ListarAsync(
        List<string>? objIncludes = null)
    {
        Respuesta<IEnumerable<TEntity>> objRespuesta = new();

        try
        {
            IQueryable<TEntity> consulta = _context.Set<TEntity>();

            if (objIncludes != null)
            {
                objIncludes.ForEach(
                    include => consulta = consulta.Include(include));
            }

            objRespuesta.Data = await consulta.ToListAsync();
        }
        catch (Exception ex)
        {
            objRespuesta.Success = false;
            objRespuesta.Error = ex.Message;
            objRespuesta.Data = null;
        }

        return objRespuesta;
    }

    public async Task<Respuesta<IEnumerable<TEntity>>> BuscarAsync(
        Expression<Func<TEntity, bool>> objPredicado,
        List<string>? objIncludes = null)
    {
        Respuesta<IEnumerable<TEntity>> objRespuesta = new();

        try
        {
            IQueryable<TEntity> consulta = _context.Set<TEntity>();

            if (objIncludes != null)
            {
                objIncludes.ForEach(
                    include => consulta = consulta.Include(include));
            }

            objRespuesta.Data = await consulta
                .Where(objPredicado)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            objRespuesta.Success = false;
            objRespuesta.Error = ex.Message;
            objRespuesta.Data = null;
        }

        return objRespuesta;
    }

    public async Task<Respuesta<TEntity>> ObtenerEntidadAsync(
        Expression<Func<TEntity, bool>> objPredicado,
        List<string>? objIncludes = null)
    {
        Respuesta<TEntity> objRespuesta = new();

        try
        {
            IQueryable<TEntity> consulta = _context.Set<TEntity>();

            if (objIncludes != null)
            {
                objIncludes.ForEach(
                    include => consulta = consulta.Include(include));
            }

            objRespuesta.Data = await consulta
                .FirstOrDefaultAsync(objPredicado);
        }
        catch (Exception ex)
        {
            objRespuesta.Success = false;
            objRespuesta.Error = ex.Message;
            objRespuesta.Data = null;
        }

        return objRespuesta;
    }

    public async Task<Respuesta<int?>> ContarAsync(
        Expression<Func<TEntity, bool>> objPredicado,
        List<string>? objIncludes = null)
    {
        Respuesta<int?> objRespuesta = new();

        try
        {
            objRespuesta.Data = await _context.Set<TEntity>()
                .CountAsync(objPredicado);
        }
        catch (Exception ex)
        {
            objRespuesta.Success = false;
            objRespuesta.Error = ex.Message;
            objRespuesta.Data = null;
        }

        return objRespuesta;
    }
}
