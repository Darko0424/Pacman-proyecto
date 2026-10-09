using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades.Vistas;

public partial class ProgresoJugador
{
    public int IdUsuario { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public int? PartidasJugadas { get; set; }

    public int PuntosTotales { get; set; }

    public int MejorPuntuacion { get; set; }

    public int NivelMaximoAlcanzado { get; set; }
}
