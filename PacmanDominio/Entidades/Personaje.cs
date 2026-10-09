using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades;

public partial class Personaje
{
    public byte IdPersonaje { get; set; }

    public string Nombre { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public string Color { get; set; } = null!;

    public virtual ICollection<ParticipantePartida> ParticipantesPartida { get; set; } = new List<ParticipantePartida>();
}
