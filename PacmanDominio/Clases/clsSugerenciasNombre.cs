using System;
using System.Collections.Generic;
using System.Text;

namespace PacmanDominio.Clases
{
    public class clsSugerenciasNombre
    {
        
            //El nombre que puso el usuario
            public string NombreUsuario { get; set; } = string.Empty;
            public bool Disponible { get; set; }

        //Nombres alternativos libres
        public List<string> Sugerencias { get; set; } = new();
        }
    }


