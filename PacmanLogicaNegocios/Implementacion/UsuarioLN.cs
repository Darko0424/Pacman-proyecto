using AutoMapper;
using Microsoft.Extensions.Logging;
using PacmanDominio.Clases;
using PacmanDominio.Entidades;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazAD;
using PacmanDominio.InterfazLN;
using PacmanUtilitarios;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace PacmanLogicaNegocios.Implementacion
{
    public class UsuarioLN : BaseLN<UsuarioLN>, IUsuarioLN
    {
        private const int FactorTrabajoHash = 12;
        private const int TamanoToken = 32;
        private const int DiasVigenciaSesion = 30;

        private const int LargoMinimoNombre = 3;
        private const int LargoMaximoNombre = 50;
        private const int LargoMaximoCorreo = 100;
        private const int LargoMinimoContrasena = 8;
        private const int LargoMaximoContrasena = 128;

        private const string ErrorCredenciales = "Usuario o contraseña incorrectos.";
        private const string ErrorSesion = "Sesión no válida.";

        public UsuarioLN(
            IUnidadTrabajoEF unidadTrabajo,
            ILogger<UsuarioLN> logger,
            IMapper mapper)
            : base(unidadTrabajo, mapper, logger)
        {
        }

        public async Task<Respuesta<TUsuario>> RegistrarAsync(
            clsRegistroUsuario datos)
        {
            var resultado = new Respuesta<TUsuario>();

            var nombreUsuario = (datos.NombreUsuario ?? string.Empty).Trim();
            var correo = (datos.CorreoElectronico ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
            var contrasena = datos.Contrasena ?? string.Empty;

            try
            {
                var errorValidacion =
                    ValidarRegistro(nombreUsuario, correo, contrasena);

                if (errorValidacion != null)
                {
                    resultado.Success = false;
                    resultado.Error = errorValidacion;
                    return resultado;
                }

                var usuarioExistente =
                    await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(
                        x => x.NombreUsuario == nombreUsuario ||
                             x.CorreoElectronico == correo);

                if (!usuarioExistente.Success)
                {
                    resultado.Success = false;
                    resultado.Error = usuarioExistente.Error;
                    return resultado;
                }

                if (usuarioExistente.Data != null)
                {
                    resultado.Success = false;
                    resultado.Error = string.Equals(
                        usuarioExistente.Data.NombreUsuario,
                        nombreUsuario,
                        StringComparison.OrdinalIgnoreCase)
                        ? "Ya existe ese nombre de usuario."
                        : "Ya existe una cuenta con ese correo.";
                    return resultado;
                }

                // FechaRegistro la asigna la base de datos (DEFAULT GETDATE())
                var entidad = new Usuario
                {
                    NombreUsuario = nombreUsuario,
                    CorreoElectronico = correo,
                    ContrasenaHash = BCrypt.Net.BCrypt.EnhancedHashPassword(
                        contrasena, FactorTrabajoHash)
                };

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TUsuario.InsertarAsync(entidad);

                if (!respuestaRepositorio.Success ||
                    respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<TUsuario>(respuestaRepositorio.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al registrar usuario {NombreUsuario}",
                    nombreUsuario);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<clsSesionIniciada>> IniciarSesionAsync(
            clsCredenciales datos)
        {
            var resultado = new Respuesta<clsSesionIniciada>();

            var identificador = (datos.Usuario ?? string.Empty).Trim();
            var contrasena = datos.Contrasena ?? string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(identificador) ||
                    string.IsNullOrEmpty(contrasena))
                {
                    resultado.Success = false;
                    resultado.Error = "Usuario y contraseña son obligatorios.";
                    return resultado;
                }

                var usuarioActual =
                    await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(
                        x => x.NombreUsuario == identificador ||
                             x.CorreoElectronico == identificador);

                if (!usuarioActual.Success)
                {
                    resultado.Success = false;
                    resultado.Error = usuarioActual.Error;
                    return resultado;
                }

                if (usuarioActual.Data == null ||
                    !VerificarContrasena(
                        contrasena, usuarioActual.Data.ContrasenaHash))
                {
                    resultado.Success = false;
                    resultado.Error = ErrorCredenciales;
                    return resultado;
                }

                var token = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(TamanoToken));

                var sesion = new SesionMovil
                {
                    IdUsuario = usuarioActual.Data.IdUsuario,
                    TokenDispositivo = CalcularHashToken(token),
                    UltimoAcceso = DateTime.Now
                };

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TSesionMovil.InsertarAsync(sesion);

                if (!respuestaRepositorio.Success ||
                    respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data = new clsSesionIniciada
                {
                    Usuario = _mapper.Map<TUsuario>(usuarioActual.Data),
                    Token = token
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al iniciar sesión de {Usuario}",
                    identificador);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<TUsuario>> ValidarSesionAsync(string token)
        {
            var resultado = new Respuesta<TUsuario>();

            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    resultado.Success = false;
                    resultado.Error = ErrorSesion;
                    return resultado;
                }

                var hashToken = CalcularHashToken(token);

                var sesionActual =
                    await _unidadDeTrabajo.TSesionMovil.ObtenerEntidadAsync(
                        x => x.TokenDispositivo == hashToken);

                if (!sesionActual.Success)
                {
                    resultado.Success = false;
                    resultado.Error = sesionActual.Error;
                    return resultado;
                }

                if (sesionActual.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorSesion;
                    return resultado;
                }

                if (sesionActual.Data.UltimoAcceso <
                    DateTime.Now.AddDays(-DiasVigenciaSesion))
                {
                    await _unidadDeTrabajo.TSesionMovil.EliminarAsync(
                        sesionActual.Data);

                    resultado.Success = false;
                    resultado.Error = "La sesión expiró. Inicie sesión de nuevo.";
                    return resultado;
                }

                var idUsuario = sesionActual.Data.IdUsuario;

                var usuarioActual =
                    await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(
                        x => x.IdUsuario == idUsuario);

                if (!usuarioActual.Success)
                {
                    resultado.Success = false;
                    resultado.Error = usuarioActual.Error;
                    return resultado;
                }

                if (usuarioActual.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorSesion;
                    return resultado;
                }

                sesionActual.Data.UltimoAcceso = DateTime.Now;

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TSesionMovil.ModificarAsync(
                        sesionActual.Data);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data = _mapper.Map<TUsuario>(usuarioActual.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar la sesión.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<bool>> CerrarSesionAsync(string token)
        {
            var resultado = new Respuesta<bool>();

            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    resultado.Success = false;
                    resultado.Error = ErrorSesion;
                    return resultado;
                }

                var hashToken = CalcularHashToken(token);

                var sesionActual =
                    await _unidadDeTrabajo.TSesionMovil.ObtenerEntidadAsync(
                        x => x.TokenDispositivo == hashToken);

                if (!sesionActual.Success)
                {
                    resultado.Success = false;
                    resultado.Error = sesionActual.Error;
                    return resultado;
                }

                // Si la sesión ya no existe, cerrarla dos veces no es un error
                if (sesionActual.Data == null)
                {
                    resultado.Data = true;
                    return resultado;
                }

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TSesionMovil.EliminarAsync(
                        sesionActual.Data);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data = respuestaRepositorio.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar la sesión.");

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        private static string? ValidarRegistro(
            string nombreUsuario, string correo, string contrasena)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario) ||
                string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrEmpty(contrasena))
            {
                return "Usuario, correo y contraseña son obligatorios.";
            }

            if (nombreUsuario.Length < LargoMinimoNombre ||
                nombreUsuario.Length > LargoMaximoNombre)
            {
                return $"El nombre de usuario debe tener entre {LargoMinimoNombre} y {LargoMaximoNombre} caracteres.";
            }

            // Sin '@' para que no se confunda con un correo al iniciar sesión
            if (nombreUsuario.Contains('@') ||
                nombreUsuario.Any(char.IsWhiteSpace))
            {
                return "El nombre de usuario no puede tener espacios ni '@'.";
            }

            if (correo.Length > LargoMaximoCorreo ||
                !MailAddress.TryCreate(correo, out var direccion) ||
                direccion.Address != correo)
            {
                return "El correo electrónico no es válido.";
            }

            if (contrasena.Length < LargoMinimoContrasena ||
                contrasena.Length > LargoMaximoContrasena)
            {
                return $"La contraseña debe tener entre {LargoMinimoContrasena} y {LargoMaximoContrasena} caracteres.";
            }

            return null;
        }

        private static bool VerificarContrasena(string contrasena, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.EnhancedVerify(contrasena, hash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // El hash guardado no tiene formato BCrypt (p. ej. datos de prueba)
                return false;
            }
        }

        // En la base se guarda el hash del token, no el token
        private static string CalcularHashToken(string token)
        {
            return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}
