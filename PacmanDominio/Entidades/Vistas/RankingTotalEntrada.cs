using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades.Vistas;

public partial class RankingTotalEntrada
{
    public int IdUsuario { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public int PuntosTotales { get; set; }

    public int PartidasJugadas { get; set; }

    public long? Posicion { get; set; }
}
