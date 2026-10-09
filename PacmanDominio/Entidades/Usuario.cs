using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string CorreoElectronico { get; set; } = null!;

    public string ContrasenaHash { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<ParticipantePartida> ParticipantesPartida { get; set; } = new List<ParticipantePartida>();

    public virtual ICollection<SesionMovil> SesionesMoviles { get; set; } = new List<SesionMovil>();
}
