using System.Net.Mail;

namespace PacmanApi.Servicios
{
    public sealed class ConfiguracionCorreo
    {
        public string Servidor { get; set; } = "smtp.gmail.com";
        public int Puerto { get; set; } = 587;
        public string Usuario { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string NombreRemitente { get; set; } = "Pac-Man";
    }
}
