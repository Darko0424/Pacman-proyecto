using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades.Vistas;

public partial class HistorialEntrada
{
    public int? IdUsuario { get; set; }

    public int IdPartida { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string Modo { get; set; } = null!;

    public string Dificultad { get; set; } = null!;

    public int NivelAlcanzado { get; set; }

    public string Personaje { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public int Puntuacion { get; set; }

    public int FantasmasComidos { get; set; }

    public int PacmansAtrapados { get; set; }

    public int FrutasComidas { get; set; }

    public string Estado { get; set; } = null!;

    public int? Gano { get; set; }
}
