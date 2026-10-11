using AutoMapper;
using Microsoft.Extensions.Logging;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazAD;
using PacmanDominio.InterfazLN;
using PacmanUtilitarios;

namespace PacmanLogicaNegocios.Implementacion
{
    public class NivelLN : BaseLN<NivelLN>, INivelLN
    {
        public NivelLN(
            IUnidadTrabajoEF unidadTrabajo,
            ILogger<NivelLN> logger,
            IMapper mapper)
            : base(unidadTrabajo, mapper, logger)
        {
        }

        public async Task<Respuesta<IEnumerable<TNivel>>> ListarAsync()
        {
            var resultado = new Respuesta<IEnumerable<TNivel>>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.TNivel.ListarAsync();

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TNivel>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.Numero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar nivel.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<IEnumerable<TNivel>>> BuscarAsync(
            TNivel datos)
        {
            var resultado = new Respuesta<IEnumerable<TNivel>>();

            try
            {
                var texto = datos.Nombre ?? string.Empty;

                if (string.IsNullOrWhiteSpace(texto))
                {
                    return await ListarAsync();
                }

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TNivel.BuscarAsync(
                        x => x.Nombre.Contains(texto));

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TNivel>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.Numero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar nivel.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<TNivel>> ObtenerAsync(TNivel datos)
        {
            var resultado = new Respuesta<TNivel>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.TNivel.ObtenerEntidadAsync(
                        x => x.IdNivel == datos.IdNivel);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                if (respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Nivel no encontrado.";
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<TNivel>(respuestaRepositorio.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener IdNivel {IdNivel}",
                    datos.IdNivel);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }
    }
}
