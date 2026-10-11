using PacmanDominio.EntidadesTipadas;

namespace PacmanDominio.Clases
{
    public class clsSesionIniciada
    {
        public TUsuario Usuario { get; set; } = new();

        // Se envía en el encabezado "Authorization: Bearer {Token}"
        public string Token { get; set; } = string.Empty;
    }
}
