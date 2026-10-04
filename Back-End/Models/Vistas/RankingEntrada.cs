using System;
using System.Collections.Generic;

namespace Back_End.Models.Vistas;

public partial class RankingEntrada
{
    public string Rol { get; set; } = null!;

    public int IdUsuario { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public int? MejorPuntuacion { get; set; }

    public int? PartidasEnRol { get; set; }

    public long? Posicion { get; set; }
}
