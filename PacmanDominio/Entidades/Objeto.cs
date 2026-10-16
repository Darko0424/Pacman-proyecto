using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades;

public partial class Objeto
{
    public byte IdObjeto { get; set; }

    public string Nombre { get; set; } = null!;

    public string Bando { get; set; } = null!;

    public string Efecto { get; set; } = null!;

    public byte DuracionSeg { get; set; }
}
