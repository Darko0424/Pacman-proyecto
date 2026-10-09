using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades;

public partial class Nivel
{
    public int IdNivel { get; set; }

    public int Numero { get; set; }

    public string Nombre { get; set; } = null!;

    public string LaberintoJson { get; set; } = null!;

    public decimal VelocidadBase { get; set; }

    public virtual ICollection<Partida> Partidas { get; set; } = new List<Partida>();
}
