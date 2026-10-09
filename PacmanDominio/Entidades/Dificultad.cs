using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades;

public partial class Dificultad
{
    public byte IdDificultad { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal MultiplicadorVelocidadIa { get; set; }

    public byte DuracionAsustadoSeg { get; set; }

    public virtual ICollection<Partida> Partidas { get; set; } = new List<Partida>();
}
