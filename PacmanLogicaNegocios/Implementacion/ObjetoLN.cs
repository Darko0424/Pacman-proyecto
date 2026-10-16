using AutoMapper;
using Microsoft.Extensions.Logging;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazAD;
using PacmanDominio.InterfazLN;
using PacmanDominio.Juego;
using PacmanUtilitarios;

namespace PacmanLogicaNegocios.Implementacion
{
    public class ObjetoLN : BaseLN<ObjetoLN>, IObjetoLN
    {
        public ObjetoLN(
            IUnidadTrabajoEF unidadTrabajo,
            ILogger<ObjetoLN> logger,
            IMapper mapper)
            : base(unidadTrabajo, mapper, logger)
        {
        }

        public async Task<Respuesta<IEnumerable<TObjeto>>> ListarAsync()
        {
            var resultado = new Respuesta<IEnumerable<TObjeto>>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.TObjeto.ListarAsync();

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TObjeto>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.IdObjeto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar objetos.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<IEnumerable<TObjeto>>> BuscarAsync(
            TObjeto datos)
        {
            var resultado = new Respuesta<IEnumerable<TObjeto>>();

            try
            {
                var texto = datos.Nombre ?? string.Empty;

                if (string.IsNullOrWhiteSpace(texto))
                {
                    return await ListarAsync();
                }

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TObjeto.BuscarAsync(
                        x => x.Nombre.Contains(texto));

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TObjeto>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.IdObjeto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar objetos.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<TObjeto>> ObtenerAsync(TObjeto datos)
        {
            var resultado = new Respuesta<TObjeto>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.TObjeto.ObtenerEntidadAsync(
                        x => x.IdObjeto == datos.IdObjeto);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                if (respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Objeto no encontrado.";
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<TObjeto>(respuestaRepositorio.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener IdObjeto {IdObjeto}",
                    datos.IdObjeto);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<IEnumerable<TObjeto>>> ListarPorBandoAsync(string bando)
        {
            var resultado = new Respuesta<IEnumerable<TObjeto>>();

            try
            {
                var bandoValido = CatalogoPersonajes.DesdeCodigo(bando);

                if (bandoValido == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Bando no válido. Use PACMAN o FANTASMA.";
                    return resultado;
                }

                var codigo = CatalogoPersonajes.ACodigo(bandoValido.Value);

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TObjeto.BuscarAsync(
                        x => x.Bando == codigo);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TObjeto>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.IdObjeto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar objetos del bando {Bando}", bando);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }
    }
}
