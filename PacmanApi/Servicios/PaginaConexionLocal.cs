using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using QRCoder;

namespace PacmanApi.Servicios;

// Página de emparejamiento para mostrar en la PC que ejecuta PacmanApi.
// Se calcula al abrirla para reflejar la IP y el túnel activo de Visual Studio.
public static class PaginaConexionLocal
{
    public static void ProgramarAperturaPaginaConexion(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Pacman:AbrirQrAlIniciar") ||
            !OperatingSystem.IsWindows() ||
            !Environment.UserInteractive)
        {
            return;
        }

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:5253/conectar",
                    UseShellExecute = true
                });

                app.Logger.LogInformation("Página QR abierta en el navegador.");
            }
            catch (Exception error)
            {
                app.Logger.LogWarning(error,
                    "No se pudo abrir la página QR automáticamente. Abre http://localhost:5253/conectar.");
            }
        });
    }

    public static IEndpointRouteBuilder MapPaginaConexionLocal(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/conectar", (HttpContext contexto) =>
        {
            contexto.Response.Headers.CacheControl = "no-store";
            return Results.Content(CrearPagina(), "text/html; charset=utf-8");
        });

        return endpoints;
    }

    private static string CrearPagina()
    {
        var pagina = new StringBuilder("""
            <!doctype html>
            <html lang="es">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Conectar Pac-Man</title>
                <style>
                    body {
                        margin: 0;
                        padding: 24px;
                        font-family: system-ui, sans-serif;
                        color: #fff;
                        background: #272736;
                        text-align: center;
                    }
                    h1 {
                        color: #ffe478;
                    }
                    .tarjetas {
                        display: flex;
                        flex-wrap: wrap;
                        justify-content: center;
                        gap: 20px;
                    }
                    .tarjeta {
                        max-width: 360px;
                        padding: 24px;
                        border: 3px solid #4b5bab;
                        border-radius: 12px;
                        background: #302b43;
                    }
                    img {
                        display: block;
                        width: min(260px, 100%);
                        height: auto;
                        margin: 18px auto;
                        padding: 12px;
                        box-sizing: border-box;
                        background: #fff;
                    }
                    code {
                        overflow-wrap: anywhere;
                        color: #79baff;
                    }
                    .ayuda {
                        max-width: 680px;
                        margin: 24px auto;
                        line-height: 1.5;
                    }
                </style>
            </head>
            <body>
                <h1>Conectar Pac-Man</h1>
                <p class="ayuda">En el inicio de sesión del juego, toca SERVIDOR y luego ESCANEAR QR.
                Para jugar desde distintas redes, escanea el túnel. Si todos comparten Wi-Fi,
                escanea la tarjeta de la red local.</p>
            """);

        var urlTunel = ObtenerUrlTunel();

        if (urlTunel is null)
        {
            pagina.Append("<p class=\"ayuda\">No hay un túnel activo. En Visual Studio, abre " +
                "Dev Tunnels desde el menú de inicio, crea uno persistente con acceso Público " +
                "para las pruebas del equipo y actívalo. " +
                "Después reinicia PacmanApi.</p>");
        }

        pagina.Append("<div class=\"tarjetas\">");

        if (urlTunel is not null)
        {
            AgregarTarjeta(pagina, "Internet: túnel de Visual Studio", urlTunel,
                "La PC debe seguir ejecutando PacmanApi.");
        }

        var direcciones = ObtenerDireccionesLocales();

        foreach (var (nombre, direccion) in direcciones)
        {
            var urlApi = $"http://{direccion}:5253";
            AgregarTarjeta(pagina, $"Red local: {nombre}", urlApi,
                "La PC y el celular deben estar en la misma red.");
        }

        if (urlTunel is null && direcciones.Count == 0)
        {
            pagina.Append("<p>No se encontró una dirección de red activa. Conecta la PC a la Wi-Fi y actualiza esta página.</p>");
        }

        pagina.Append("""
                </div>
                <p class="ayuda">Esta página solo comparte la dirección de la API.
                No incluye usuarios, contraseñas ni datos de la base de datos.</p>
            </body>
            </html>
            """);

        return pagina.ToString();
    }

    private static void AgregarTarjeta(StringBuilder pagina, string nombre, string urlApi,
        string explicacion)
    {
        var enlaceQr = $"pacman://servidor?url={Uri.EscapeDataString(urlApi)}";
        var imagen = PngByteQRCodeHelper.GetQRCode(
            enlaceQr,
            QRCodeGenerator.ECCLevel.Q,
            8
        );

        pagina.Append("<section class=\"tarjeta\"><h2>")
            .Append(WebUtility.HtmlEncode(nombre))
            .Append("</h2><p>")
            .Append(WebUtility.HtmlEncode(explicacion))
            .Append("</p><img alt=\"QR del servidor\" src=\"data:image/png;base64,")
            .Append(Convert.ToBase64String(imagen))
            .Append("\"><p>Dirección para escribirla a mano:<br><code>")
            .Append(WebUtility.HtmlEncode(urlApi))
            .Append("</code></p></section>");
    }

    private static string? ObtenerUrlTunel()
    {
        var valor = Environment.GetEnvironmentVariable("VS_TUNNEL_URL");

        if (!Uri.TryCreate(valor, UriKind.Absolute, out var url) ||
            url.Scheme != Uri.UriSchemeHttps ||
            !url.Host.EndsWith(".devtunnels.ms", StringComparison.OrdinalIgnoreCase) ||
            !url.IsDefaultPort ||
            !string.IsNullOrEmpty(url.UserInfo))
        {
            return null;
        }

        return url.GetLeftPart(UriPartial.Authority);
    }

    private static List<(string Nombre, IPAddress Direccion)> ObtenerDireccionesLocales()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(red => red.OperationalStatus == OperationalStatus.Up)
            .Where(red => red.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(red => red.GetIPProperties().UnicastAddresses
                .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
                .Where(ip => !IPAddress.IsLoopback(ip.Address) &&
                    !ip.Address.ToString().StartsWith("169.254.", StringComparison.Ordinal))
                .Select(ip => (Nombre: red.Name, Direccion: ip.Address,
                    Preferencia: red.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 0 : 1)))
            .DistinctBy(red => red.Direccion)
            .OrderBy(red => red.Preferencia)
            .ThenBy(red => red.Nombre)
            .Select(red => (red.Nombre, red.Direccion))
            .ToList();
    }
}
