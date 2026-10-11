using AutoMapper;
using Microsoft.Extensions.Logging;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazAD;
using PacmanDominio.InterfazLN;
using PacmanUtilitarios;

namespace PacmanLogicaNegocios.Implementacion
{
    public class DificultadLN : BaseLN<DificultadLN>, IDificultadLN
    {
        public DificultadLN(
            IUnidadTrabajoEF unidadTrabajo,
            ILogger<DificultadLN> logger,
            IMapper mapper)
            : base(unidadTrabajo, mapper, logger)
        {
        }

        public async Task<Respuesta<IEnumerable<TDificultad>>> ListarAsync()
        {
            var resultado = new Respuesta<IEnumerable<TDificultad>>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.TDificultad.ListarAsync();

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TDificultad>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.IdDificultad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar dificultad.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<IEnumerable<TDificultad>>> BuscarAsync(
            TDificultad datos)
        {
            var resultado = new Respuesta<IEnumerable<TDificultad>>();

            try
            {
                var texto = datos.Nombre ?? string.Empty;

                if (string.IsNullOrWhiteSpace(texto))
                {
                    return await ListarAsync();
                }

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TDificultad.BuscarAsync(
                        x => x.Nombre.Contains(texto));

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TDificultad>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.IdDificultad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar dificultad.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<TDificultad>> ObtenerAsync(TDificultad datos)
        {
            var resultado = new Respuesta<TDificultad>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.TDificultad.ObtenerEntidadAsync(
                        x => x.IdDificultad == datos.IdDificultad);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                if (respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Dificultad no encontrada.";
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<TDificultad>(respuestaRepositorio.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener IdDificultad {IdDificultad}",
                    datos.IdDificultad);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }
    }
}
