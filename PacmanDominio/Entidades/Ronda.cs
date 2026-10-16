using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades;

public partial class Ronda
{
    public int IdRonda { get; set; }

    public int IdPartida { get; set; }

    public int IdNivel { get; set; }

    public string? BandoGanador { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public virtual Partida IdPartidaNavigation { get; set; } = null!;

    public virtual Nivel IdNivelNavigation { get; set; } = null!;

    public virtual ICollection<ParticipanteRonda> ParticipantesRonda { get; set; } = new List<ParticipanteRonda>();
}
