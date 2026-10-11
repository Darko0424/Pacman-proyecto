using AutoMapper;
using Microsoft.Extensions.Logging;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazAD;
using PacmanDominio.InterfazLN;
using PacmanUtilitarios;

namespace PacmanLogicaNegocios.Implementacion
{
    public class PersonajeLN : BaseLN<PersonajeLN>, IPersonajeLN
    {
        public PersonajeLN(
            IUnidadTrabajoEF unidadTrabajo,
            ILogger<PersonajeLN> logger,
            IMapper mapper)
            : base(unidadTrabajo, mapper, logger)
        {
        }

        public async Task<Respuesta<IEnumerable<TPersonaje>>> ListarAsync()
        {
            var resultado = new Respuesta<IEnumerable<TPersonaje>>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.TPersonaje.ListarAsync();

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TPersonaje>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.IdPersonaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar personaje.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<IEnumerable<TPersonaje>>> BuscarAsync(
            TPersonaje datos)
        {
            var resultado = new Respuesta<IEnumerable<TPersonaje>>();

            try
            {
                var texto = datos.Nombre ?? string.Empty;

                if (string.IsNullOrWhiteSpace(texto))
                {
                    return await ListarAsync();
                }

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TPersonaje.BuscarAsync(
                        x => x.Nombre.Contains(texto));

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TPersonaje>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.IdPersonaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar personaje.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<TPersonaje>> ObtenerAsync(TPersonaje datos)
        {
            var resultado = new Respuesta<TPersonaje>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.TPersonaje.ObtenerEntidadAsync(
                        x => x.IdPersonaje == datos.IdPersonaje);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                if (respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Personaje no encontrado.";
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<TPersonaje>(respuestaRepositorio.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener IdPersonaje {IdPersonaje}",
                    datos.IdPersonaje);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }
    }
}
