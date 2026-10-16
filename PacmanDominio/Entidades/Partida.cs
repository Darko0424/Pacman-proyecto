using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades;

public partial class Partida
{
    public int IdPartida { get; set; }

    public string CodigoSala { get; set; } = null!;

    public string Modo { get; set; } = null!;

    public byte IdDificultad { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public virtual Dificultad IdDificultadNavigation { get; set; } = null!;

    public virtual ICollection<Ronda> Rondas { get; set; } = new List<Ronda>();
}
