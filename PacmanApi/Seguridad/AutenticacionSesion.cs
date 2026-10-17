using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PacmanApi.Extensiones;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Seguridad;

// Valida el token de api/Usuario/IniciarSesion para el hub de juego.
// El cliente de SignalR lo envía en el encabezado Authorization o, en WebSockets
// (donde el navegador no permite encabezados), como ?access_token=.
public sealed class AutenticacionSesion : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Esquema = "SesionPacman";

    private readonly IUsuarioLN _usuarioLN;

    public AutenticacionSesion(
        IOptionsMonitor<AuthenticationSchemeOptions> opciones,
        ILoggerFactory registro,
        UrlEncoder codificador,
        IUsuarioLN usuarioLN)
        : base(opciones, registro, codificador)
    {
        _usuarioLN = usuarioLN;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.ObtenerToken();

        // El token en la URL solo se acepta para los hubs
        if (string.IsNullOrEmpty(token) && Request.Path.StartsWithSegments("/hubs"))
        {
            token = Request.Query["access_token"].ToString();
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var sesion = await _usuarioLN.ValidarSesionAsync(token);

        if (!sesion.Success || sesion.Data == null)
        {
            return AuthenticateResult.Fail(sesion.Error);
        }

        var identidad = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, sesion.Data.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, sesion.Data.NombreUsuario)
            },
            Esquema);

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identidad), Esquema));
    }
}
