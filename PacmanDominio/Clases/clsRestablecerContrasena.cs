using System;
using System.Collections.Generic;
using System.Text;

namespace PacmanDominio.Clases
{
    public class clsRestablecerContrasena
    {
        public string CorreoElectronico { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string ContrasenaNueva { get; set; } = string.Empty;
    }
}
