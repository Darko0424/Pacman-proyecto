using AutoMapper;
using Microsoft.Extensions.Logging;
using PacmanDominio.Entidades.Vistas;
using PacmanDominio.InterfazAD;
using PacmanDominio.InterfazLN;
using PacmanUtilitarios;

namespace PacmanLogicaNegocios.Implementacion
{
    public class RankingLN : BaseLN<RankingLN>, IRankingLN
    {
        // Mismos valores que el CHECK de personajes.rol
        private static readonly string[] RolesValidos = ["PACMAN", "FANTASMA"];

        private const string ErrorRol = "Rol no válido. Use PACMAN o FANTASMA.";

        public RankingLN(
            IUnidadTrabajoEF unidadTrabajo,
            ILogger<RankingLN> logger,
            IMapper mapper)
            : base(unidadTrabajo, mapper, logger)
        {
        }

        public async Task<Respuesta<IEnumerable<RankingEntrada>>> ListarPorRolAsync(
            string rol)
        {
            var resultado = new Respuesta<IEnumerable<RankingEntrada>>();

            var rolNormalizado = NormalizarRol(rol);

            try
            {
                if (rolNormalizado == null)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorRol;
                    return resultado;
                }

                var respuestaRepositorio =
                    await _unidadDeTrabajo.VRanking.BuscarAsync(
                        x => x.Rol == rolNormalizado);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data = (respuestaRepositorio.Data ?? [])
                    .OrderBy(x => x.Posicion)
                    .ThenBy(x => x.NombreUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar ranking del rol {Rol}", rol);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<RankingEntrada>> ObtenerPosicionAsync(
            int idUsuario, string rol)
        {
            var resultado = new Respuesta<RankingEntrada>();

            var rolNormalizado = NormalizarRol(rol);

            try
            {
                if (idUsuario <= 0)
                {
                    resultado.Success = false;
                    resultado.Error = "Usuario no válido.";
                    return resultado;
                }

                if (rolNormalizado == null)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorRol;
                    return resultado;
                }

                // RANK() se calcula dentro de la vista, así que filtrar por
                // usuario no altera la posición
                var respuestaRepositorio =
                    await _unidadDeTrabajo.VRanking.ObtenerEntidadAsync(
                        x => x.IdUsuario == idUsuario && x.Rol == rolNormalizado);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                if (respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error =
                        "El usuario aún no tiene partidas multijugador finalizadas con ese rol.";
                    return resultado;
                }

                resultado.Data = respuestaRepositorio.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener posición del IdUsuario {IdUsuario} en {Rol}",
                    idUsuario,
                    rol);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<IEnumerable<RankingTotalEntrada>>> ListarTotalAsync()
        {
            var resultado = new Respuesta<IEnumerable<RankingTotalEntrada>>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.VRankingTotal.ListarAsync();

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data = (respuestaRepositorio.Data ?? [])
                    .OrderBy(x => x.Posicion)
                    .ThenBy(x => x.NombreUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar el ranking total.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<RankingTotalEntrada>> ObtenerPosicionTotalAsync(int idUsuario)
        {
            var resultado = new Respuesta<RankingTotalEntrada>();

            try
            {
                if (idUsuario <= 0)
                {
                    resultado.Success = false;
                    resultado.Error = "Usuario no válido.";
                    return resultado;
                }

                // RANK() se calcula dentro de la vista, así que filtrar por
                // usuario no altera la posición
                var respuestaRepositorio =
                    await _unidadDeTrabajo.VRankingTotal.ObtenerEntidadAsync(
                        x => x.IdUsuario == idUsuario);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                if (respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error =
                        "El usuario aún no tiene partidas multijugador finalizadas.";
                    return resultado;
                }

                resultado.Data = respuestaRepositorio.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener posición total del IdUsuario {IdUsuario}",
                    idUsuario);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        private static string? NormalizarRol(string? rol)
        {
            var valor = (rol ?? string.Empty).Trim().ToUpperInvariant();

            return RolesValidos.Contains(valor) ? valor : null;
        }
    }
}
