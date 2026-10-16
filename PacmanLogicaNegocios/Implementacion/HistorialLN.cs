using AutoMapper;
using Microsoft.Extensions.Logging;
using PacmanDominio.Entidades.Vistas;
using PacmanDominio.InterfazAD;
using PacmanDominio.InterfazLN;
using PacmanUtilitarios;

namespace PacmanLogicaNegocios.Implementacion
{
    public class HistorialLN : BaseLN<HistorialLN>, IHistorialLN
    {
        private const string ErrorUsuario = "Usuario no válido.";

        public HistorialLN(
            IUnidadTrabajoEF unidadTrabajo,
            ILogger<HistorialLN> logger,
            IMapper mapper)
            : base(unidadTrabajo, mapper, logger)
        {
        }

        public async Task<Respuesta<IEnumerable<HistorialEntrada>>> ListarPorUsuarioAsync(
            int idUsuario)
        {
            var resultado = new Respuesta<IEnumerable<HistorialEntrada>>();

            try
            {
                if (idUsuario <= 0)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorUsuario;
                    return resultado;
                }

                var respuestaRepositorio =
                    await _unidadDeTrabajo.VHistorialUsuario.BuscarAsync(
                        x => x.IdUsuario == idUsuario);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                // Las partidas más recientes primero
                resultado.Data = (respuestaRepositorio.Data ?? [])
                    .OrderByDescending(x => x.FechaInicio)
                    .ThenByDescending(x => x.IdPartida);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al listar historial del IdUsuario {IdUsuario}",
                    idUsuario);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<IEnumerable<HistorialRonda>>> ListarRondasAsync(
            int idUsuario, int idPartida)
        {
            var resultado = new Respuesta<IEnumerable<HistorialRonda>>();

            try
            {
                if (idUsuario <= 0)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorUsuario;
                    return resultado;
                }

                // Filtrar también por usuario: solo ve el detalle de sus partidas
                var respuestaRepositorio =
                    await _unidadDeTrabajo.VHistorialRondas.BuscarAsync(
                        x => x.IdUsuario == idUsuario && x.IdPartida == idPartida);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                var rondas = (respuestaRepositorio.Data ?? []).ToList();

                if (rondas.Count == 0)
                {
                    resultado.Success = false;
                    resultado.Error = "Partida no encontrada en el historial del usuario.";
                    return resultado;
                }

                resultado.Data = rondas.OrderBy(x => x.NumeroNivel);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al listar niveles de IdPartida {IdPartida} del IdUsuario {IdUsuario}",
                    idPartida,
                    idUsuario);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<ProgresoJugador>> ObtenerProgresoAsync(int idUsuario)
        {
            var resultado = new Respuesta<ProgresoJugador>();

            try
            {
                if (idUsuario <= 0)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorUsuario;
                    return resultado;
                }

                var respuestaRepositorio =
                    await _unidadDeTrabajo.VProgresoJugador.ObtenerEntidadAsync(
                        x => x.IdUsuario == idUsuario);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                // La vista incluye a todo usuario registrado, aunque no haya jugado
                if (respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Usuario no encontrado.";
                    return resultado;
                }

                resultado.Data = respuestaRepositorio.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener progreso del IdUsuario {IdUsuario}",
                    idUsuario);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }
    }
}
