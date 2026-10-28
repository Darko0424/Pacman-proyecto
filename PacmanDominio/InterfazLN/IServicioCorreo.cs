using System;
using System.Collections.Generic;
using System.Text;

namespace PacmanDominio.InterfazLN
{
    public interface IServicioCorreo 
    {
        Task<bool> EnviarCodigoVerificacionAsync(string destinatario, string codigo, string proposito = "VERIFICAR");
    }
}
