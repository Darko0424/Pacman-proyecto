using System;
using System.Collections.Generic;

namespace Back_End.Models;

public partial class Fruta
{
    public byte IdFruta { get; set; }

    public string Nombre { get; set; } = null!;

    public string Efecto { get; set; } = null!;

    public byte DuracionSeg { get; set; }

    public int Puntos { get; set; }
}
