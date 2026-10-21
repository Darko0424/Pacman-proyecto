using MailKit.Security;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Servicios
{
    public sealed class ServicioCorreoSmtp : IServicioCorreo
    {
        private readonly ConfiguracionCorreo _configuracion;
        private readonly IHostEnvironment _entorno;
        private readonly ILogger<ServicioCorreoSmtp> _logger;

        public ServicioCorreoSmtp(
        IOptions<ConfiguracionCorreo> configuracion,
        IHostEnvironment entorno,
        ILogger<ServicioCorreoSmtp> logger)
        {
            _configuracion = configuracion.Value;
            _entorno = entorno;
            _logger = logger;
        }

        public async Task<bool> EnviarCodigoVerificacionAsync(string destinatario, string codigo)
        {
            var usuario = _configuracion.Usuario.Trim();
            // Google muestra la clave como "abcd efgh ijkl mnop": se quitan los espacios
            var contrasena = _configuracion.Contrasena.Replace(" ", string.Empty);

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
            {
                if (_entorno.IsDevelopment())
                {
                    // Para probar sin la clave de Gmail: el código sale en la consola de la API
                    _logger.LogWarning(
                        "Correo sin configurar. Código de {Destinatario}: {Codigo}",
                        destinatario, codigo);
                    return true;
                }

                _logger.LogError("Falta configurar la sección Correo.");
                return false;
            }

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(_configuracion.NombreRemitente, usuario));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = $"{codigo} es tu código de Pac-Man";
            mensaje.Body = new BodyBuilder { HtmlBody = CrearCuerpo(codigo) }.ToMessageBody();

            using var cliente = new SmtpClient();

            try
            {
                await cliente.ConnectAsync(
                    _configuracion.Servidor, _configuracion.Puerto, SecureSocketOptions.StartTls);
                await cliente.AuthenticateAsync(usuario, contrasena);
                await cliente.SendAsync(mensaje);
                await cliente.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                // El motivo va en la primera línea para no tener que leer toda la traza
                _logger.LogError(
                    ex,
                    "No se pudo enviar el correo a {Destinatario}. Motivo: {Motivo}",
                    destinatario, ex.Message);
                return false;
            }
        }
        private static string CrearCuerpo(string codigo) => $"""
            <div style="font-family:Arial,sans-serif;background:#272736;color:#ffffff;padding:32px;text-align:center">
                <h1 style="color:#ffe478;margin:0 0 16px">PAC-MAN</h1>
                <p>Tu código de verificación es:</p>
                <p style="font-size:36px;letter-spacing:8px;font-weight:bold;color:#4da6ff;margin:16px 0">{codigo}</p>
                <p style="color:#c2c2d1">Vence en 15 minutos. Si no creaste una cuenta, ignora este correo.</p>
            </div>
            """;
    }
}

