using System;
using System.Collections.Generic;

namespace Back_End.Models;

public partial class SesionMovil
{
    public int IdSesion { get; set; }

    public int IdUsuario { get; set; }

    public string TokenDispositivo { get; set; } = null!;

    public DateTime UltimoAcceso { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
