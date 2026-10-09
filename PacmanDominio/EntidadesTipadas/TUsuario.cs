using System.Text.Json.Serialization;

namespace PacmanDominio.EntidadesTipadas;

public class TUsuario
{
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Contrasena { get; set; }
}
