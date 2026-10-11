using AutoMapper;
using Microsoft.Extensions.Logging;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazAD;
using PacmanDominio.InterfazLN;
using PacmanUtilitarios;

namespace PacmanLogicaNegocios.Implementacion
{
    public class FrutaLN : BaseLN<FrutaLN>, IFrutaLN
    {
        public FrutaLN(
            IUnidadTrabajoEF unidadTrabajo,
            ILogger<FrutaLN> logger,
            IMapper mapper)
            : base(unidadTrabajo, mapper, logger)
        {
        }

        public async Task<Respuesta<IEnumerable<TFruta>>> ListarAsync()
        {
            var resultado = new Respuesta<IEnumerable<TFruta>>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.TFruta.ListarAsync();

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TFruta>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.IdFruta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar fruta.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<IEnumerable<TFruta>>> BuscarAsync(
            TFruta datos)
        {
            var resultado = new Respuesta<IEnumerable<TFruta>>();

            try
            {
                var texto = datos.Nombre ?? string.Empty;

                if (string.IsNullOrWhiteSpace(texto))
                {
                    return await ListarAsync();
                }

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TFruta.BuscarAsync(
                        x => x.Nombre.Contains(texto));

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TFruta>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.IdFruta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar fruta.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<TFruta>> ObtenerAsync(TFruta datos)
        {
            var resultado = new Respuesta<TFruta>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.TFruta.ObtenerEntidadAsync(
                        x => x.IdFruta == datos.IdFruta);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                if (respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Fruta no encontrada.";
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<TFruta>(respuestaRepositorio.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener IdFruta {IdFruta}",
                    datos.IdFruta);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }
    }
}
