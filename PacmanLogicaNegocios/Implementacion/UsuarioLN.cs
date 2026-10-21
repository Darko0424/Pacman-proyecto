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
        // El front revisa este código para mandar a la pantalla de verificación
        public const string CodigoCorreoNoVerificado = "CORREO_NO_VERIFICADO";

        private const int FactorTrabajoHash = 12;
        private const int FactorTrabajoCodigo = 10;
        private const int TamanoToken = 32;
        private const int DiasVigenciaSesion = 30;

        private const int LargoMinimoNombre = 3;
        private const int LargoMaximoNombre = 50;
        private const int LargoMaximoCorreo = 100;
        private const int LargoMinimoContrasena = 8;
        private const int LargoMaximoContrasena = 128;

        private const int MinutosVigenciaCodigo = 15;
        private const int SegundosEsperaReenvio = 60;
        private const int MaximoIntentosCodigo = 5;
        private const int CantidadSugerencias = 5;

        private const string ErrorCredenciales = "Usuario o contraseña incorrectos.";
        private const string ErrorSesion = "Sesión no válida.";
        private const string ErrorCodigo = "El código no es válido.";
        private const string ErrorYaVerificada = "Esta cuenta ya está verificada. Inicia sesión.";

        private static readonly string[] SufijosNombre = { "xd", "_gg", "_pro", "_cr", "_yt" };

        private readonly IServicioCorreo _servicioCorreo;

        public UsuarioLN(
           IUnidadTrabajoEF unidadTrabajo,
           ILogger<UsuarioLN> logger,
           IMapper mapper,
           IServicioCorreo servicioCorreo)
           : base(unidadTrabajo, mapper, logger)
        {
            _servicioCorreo = servicioCorreo;
        }

        // ---------- Registro y verificación ----------

        public async Task<Respuesta<TUsuario>> RegistrarAsync(
            clsRegistroUsuario datos)
        {
            var resultado = new Respuesta<TUsuario>();

            var correo = NormalizarCorreo(datos.CorreoElectronico);
            var contrasena = datos.Contrasena ?? string.Empty;

            try
            {
                var errorValidacion = ValidarRegistro(correo, contrasena);

                if (errorValidacion != null)
                {
                    resultado.Success = false;
                    resultado.Error = errorValidacion;
                    return resultado;
                }

                var usuarioExistente =
                    await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(
                        x => x.CorreoElectronico == correo);

                if (!usuarioExistente.Success)
                {
                    resultado.Success = false;
                    resultado.Error = usuarioExistente.Error;
                    return resultado;
                }

                if (usuarioExistente.Data != null)
                {
                    resultado.Success = false;
                    resultado.Error = "Ya existe una cuenta con ese correo.";
                    return resultado;
                }

                // El nombre definitivo lo elige después de iniciar sesión
                var nombreTemporal = await GenerarNombreTemporalAsync();

                if (nombreTemporal == null)
                {
                    resultado.Success = false;
                    resultado.Error = "No se pudo crear la cuenta. Intenta de nuevo.";
                    return resultado;
                }

                // FechaRegistro la asigna la base de datos (DEFAULT GETDATE())
                var entidad = new Usuario
                {
                    NombreUsuario = nombreTemporal,
                    CorreoElectronico = correo,
                    ContrasenaHash = BCrypt.Net.BCrypt.EnhancedHashPassword(
                        contrasena, FactorTrabajoHash),
                    CorreoVerificado = false,
                    NombreDefinido = false
                };

                var codigo = AsignarCodigoNuevo(entidad);

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

                // La cuenta ya quedó creada: si el correo falla se puede reenviar
                if (!await _servicioCorreo.EnviarCodigoVerificacionAsync(correo, codigo))
                {
                    resultado.Error =
                        "La cuenta se creó, pero no se pudo enviar el correo. Usa \"Reenviar código\".";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar el correo {Correo}", correo);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<clsSesionIniciada>> VerificarCorreoAsync(
           clsVerificacionCorreo datos)
        {
            var resultado = new Respuesta<clsSesionIniciada>();

            var correo = NormalizarCorreo(datos.CorreoElectronico);
            var codigo = (datos.Codigo ?? string.Empty).Trim();

            try
            {
                if (string.IsNullOrEmpty(correo) || codigo.Length != 6)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorCodigo;
                    return resultado;
                }

                var usuarioActual =
                    await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(
                        x => x.CorreoElectronico == correo);

                if (!usuarioActual.Success)
                {
                    resultado.Success = false;
                    resultado.Error = usuarioActual.Error;
                    return resultado;
                }

                var usuario = usuarioActual.Data;

                if (usuario == null)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorCodigo;
                    return resultado;
                }

                if (usuario.CorreoVerificado)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorYaVerificada;
                    return resultado;
                }

                if (usuario.CodigoVerificacionHash == null ||
                    usuario.CodigoExpira < DateTime.Now)
                {
                    resultado.Success = false;
                    resultado.Error = "El código venció. Pide uno nuevo.";
                    return resultado;
                }

                if (usuario.IntentosCodigo >= MaximoIntentosCodigo)
                {
                    resultado.Success = false;
                    resultado.Error = "Demasiados intentos. Pide un código nuevo.";
                    return resultado;
                }

                if (!VerificarContrasena(codigo, usuario.CodigoVerificacionHash))
                {
                    usuario.IntentosCodigo++;
                    await _unidadDeTrabajo.TUsuario.ModificarAsync(usuario);

                    var restantes = MaximoIntentosCodigo - usuario.IntentosCodigo;

                    resultado.Success = false;
                    resultado.Error = restantes > 0
                        ? $"Código incorrecto. Te quedan {restantes} intentos."
                        : "Demasiados intentos. Pide un código nuevo.";
                    return resultado;
                }

                usuario.CorreoVerificado = true;
                usuario.CodigoVerificacionHash = null;
                usuario.CodigoExpira = null;
                usuario.IntentosCodigo = 0;

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TUsuario.ModificarAsync(usuario);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                // Queda con la sesión iniciada para no pedirle la contraseña otra vez
                return await CrearSesionAsync(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar el correo {Correo}", correo);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<bool>> ReenviarCodigoAsync(
           clsSolicitudCodigo datos)
        {
            var resultado = new Respuesta<bool>();

            var correo = NormalizarCorreo(datos.CorreoElectronico);

            try
            {
                var usuarioActual =
                    await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(
                        x => x.CorreoElectronico == correo);

                if (!usuarioActual.Success)
                {
                    resultado.Success = false;
                    resultado.Error = usuarioActual.Error;
                    return resultado;
                }

                var usuario = usuarioActual.Data;

                // No se revela si el correo tiene cuenta
                if (usuario == null)
                {
                    resultado.Data = true;
                    return resultado;
                }

                if (usuario.CorreoVerificado)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorYaVerificada;
                    return resultado;
                }

                if (usuario.CodigoEnviado.HasValue)
                {
                    var espera = SegundosEsperaReenvio -
                        (int)(DateTime.Now - usuario.CodigoEnviado.Value).TotalSeconds;

                    if (espera > 0)
                    {
                        resultado.Success = false;
                        resultado.Error = $"Espera {espera} segundos para pedir otro código.";
                        return resultado;
                    }
                }

                var codigo = AsignarCodigoNuevo(usuario);

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TUsuario.ModificarAsync(usuario);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                if (!await _servicioCorreo.EnviarCodigoVerificacionAsync(correo, codigo))
                {
                    resultado.Success = false;
                    resultado.Error = "No se pudo enviar el correo. Intenta más tarde.";
                    return resultado;
                }

                resultado.Data = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reenviar el código a {Correo}", correo);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        // ---------- Sesión ----------

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

                var correo = identificador.ToLowerInvariant();

                var usuarioActual =
                    await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(
                        x => x.NombreUsuario == identificador ||
                             x.CorreoElectronico == correo);

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

                // Se revisa después de la contraseña para no revelar qué correos existen.
                // Se devuelve el usuario (sin token) para que el front sepa a qué correo
                // pertenece el código, aunque haya entrado con el nombre de usuario.
                if (!usuarioActual.Data.CorreoVerificado)
                {
                    resultado.Success = false;
                    resultado.Codigo = CodigoCorreoNoVerificado;
                    resultado.Error = "Debes verificar tu correo antes de iniciar sesión.";
                    resultado.Data = new clsSesionIniciada
                    {
                        Usuario = _mapper.Map<TUsuario>(usuarioActual.Data)
                    };
                    return resultado;
                }

                return await CrearSesionAsync(usuarioActual.Data);
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


        // ---------- Nombre de usuario ----------

        public async Task<Respuesta<clsSugerenciasNombre>> SugerirNombresAsync(
            string nombreDeseado)
        {
            var resultado = new Respuesta<clsSugerenciasNombre>();

            var nombre = (nombreDeseado ?? string.Empty).Trim();

            try
            {
                var errorValidacion = ValidarNombre(nombre);

                if (errorValidacion != null)
                {
                    resultado.Success = false;
                    resultado.Error = errorValidacion;
                    return resultado;
                }

                var candidatos = GenerarCandidatos(nombre);

                // Una sola consulta para saber cuáles ya están ocupados
                var ocupados =
                    await _unidadDeTrabajo.TUsuario.BuscarAsync(
                        x => candidatos.Contains(x.NombreUsuario));

                if (!ocupados.Success || ocupados.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = ocupados.Error;
                    return resultado;
                }

                var nombresOcupados = new HashSet<string>(
                    ocupados.Data.Select(x => x.NombreUsuario),
                    StringComparer.OrdinalIgnoreCase);

                resultado.Data = new clsSugerenciasNombre
                {
                    NombreUsuario = nombre,
                    Disponible = !nombresOcupados.Contains(nombre),
                    Sugerencias = candidatos
                        .Skip(1)
                        .Where(x => !nombresOcupados.Contains(x))
                        .Take(CantidadSugerencias)
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al sugerir nombres para {Nombre}", nombre);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<TUsuario>> AsignarNombreAsync(
           string token, clsNombreUsuario datos)
        {
            var resultado = new Respuesta<TUsuario>();

            var nombre = (datos.NombreUsuario ?? string.Empty).Trim();

            try
            {
                var sesion = await ValidarSesionAsync(token);

                if (!sesion.Success || sesion.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = sesion.Error;
                    return resultado;
                }

                var errorValidacion = ValidarNombre(nombre);

                if (errorValidacion != null)
                {
                    resultado.Success = false;
                    resultado.Error = errorValidacion;
                    return resultado;
                }

                var idUsuario = sesion.Data.IdUsuario;

                var ocupado =
                    await _unidadDeTrabajo.TUsuario.ContarAsync(
                        x => x.NombreUsuario == nombre && x.IdUsuario != idUsuario);

                if (!ocupado.Success)
                {
                    resultado.Success = false;
                    resultado.Error = ocupado.Error;
                    return resultado;
                }

                if (ocupado.Data > 0)
                {
                    resultado.Success = false;
                    resultado.Error = "Ese nombre ya está ocupado.";
                    return resultado;
                }

                var usuarioActual =
                    await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(
                        x => x.IdUsuario == idUsuario);

                if (!usuarioActual.Success || usuarioActual.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = ErrorSesion;
                    return resultado;
                }

                usuarioActual.Data.NombreUsuario = nombre;
                usuarioActual.Data.NombreDefinido = true;

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TUsuario.ModificarAsync(usuarioActual.Data);

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
                _logger.LogError(ex, "Error al asignar el nombre {Nombre}", nombre);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        // ---------- Auxiliares ----------

        private async Task<Respuesta<clsSesionIniciada>> CrearSesionAsync(Usuario usuario)
        {
            var resultado = new Respuesta<clsSesionIniciada>();

            var token = Convert.ToHexString(
                RandomNumberGenerator.GetBytes(TamanoToken));

            var sesion = new SesionMovil
            {
                IdUsuario = usuario.IdUsuario,
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
                Usuario = _mapper.Map<TUsuario>(usuario),
                Token = token
            };

            return resultado;
        }

        // Genera el código de 6 dígitos, guarda su hash y devuelve el código en claro
        // para enviarlo por correo
        private static string AsignarCodigoNuevo(Usuario usuario)
        {
            var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            usuario.CodigoVerificacionHash =
                BCrypt.Net.BCrypt.EnhancedHashPassword(codigo, FactorTrabajoCodigo);
            usuario.CodigoExpira = DateTime.Now.AddMinutes(MinutosVigenciaCodigo);
            usuario.CodigoEnviado = DateTime.Now;
            usuario.IntentosCodigo = 0;

            return codigo;
        }

        private async Task<string?> GenerarNombreTemporalAsync()
        {
            for (var intento = 0; intento < 10; intento++)
            {
                var nombre = $"jugador_{RandomNumberGenerator.GetInt32(100000, 1000000)}";

                var existe =
                    await _unidadDeTrabajo.TUsuario.ContarAsync(
                        x => x.NombreUsuario == nombre);

                if (existe.Success && existe.Data == 0)
                {
                    return nombre;
                }
            }

            return null;
        }

        // Primero el nombre pedido y después variantes: pepe7, pepe_23, pepexd...
        private static List<string> GenerarCandidatos(string nombre)
        {
            // Se recorta para que las variantes no pasen del largo máximo
            var raiz = nombre.Length > LargoMaximoNombre - 4
                ? nombre[..(LargoMaximoNombre - 4)]
                : nombre;

            var candidatos = new List<string> { nombre };
            var indiceSufijo = 0;

            for (var i = 0; i < 20; i++)
            {
                var numero = RandomNumberGenerator.GetInt32(1, 1000);

                var candidato = (i % 3) switch
                {
                    0 => $"{raiz}{numero}",
                    1 => $"{raiz}_{numero}",
                    _ => raiz + SufijosNombre[indiceSufijo++ % SufijosNombre.Length]
                };

                if (!candidatos.Contains(candidato, StringComparer.OrdinalIgnoreCase))
                {
                    candidatos.Add(candidato);
                }
            }

            return candidatos;
        }

        private static string NormalizarCorreo(string? correo)
        {
            return (correo ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string? ValidarRegistro(string correo, string contrasena)
        {
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrEmpty(contrasena))
            {
                return "Correo y contraseña son obligatorios.";
            }

            // Acepta cualquier dominio: solo se revisa el formato
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


        private static string? ValidarNombre(string nombre)
        {
            if (nombre.Length < LargoMinimoNombre || nombre.Length > LargoMaximoNombre)
            {
                return $"El nombre debe tener entre {LargoMinimoNombre} y {LargoMaximoNombre} caracteres.";
            }

            // Letras sin tilde (la fuente del juego no las tiene en mayúscula), números,
            // '_' y '.'. Sin '@' para que no se confunda con un correo al iniciar sesión.
            if (!nombre.All(c => char.IsAsciiLetterOrDigit(c) || c == '_' || c == '.'))
            {
                return "El nombre solo puede tener letras, números, '_' y '.'.";
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
