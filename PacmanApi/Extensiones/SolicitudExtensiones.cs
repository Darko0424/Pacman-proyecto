namespace PacmanApi.Extensiones;

public static class SolicitudExtensiones
{
    private const string PrefijoToken = "Bearer ";

    // Token de api/Usuario/IniciarSesion enviado en el encabezado Authorization
    public static string ObtenerToken(this HttpRequest solicitud)
    {
        var encabezado = solicitud.Headers.Authorization.ToString();

        return encabezado.StartsWith(PrefijoToken, StringComparison.OrdinalIgnoreCase)
            ? encabezado[PrefijoToken.Length..].Trim()
            : string.Empty;
    }
}
