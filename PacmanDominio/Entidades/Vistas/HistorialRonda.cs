using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades.Vistas;

// Detalle del historial: una fila por usuario y nivel jugado
public partial class HistorialRonda
{
    public int? IdUsuario { get; set; }

    public int IdPartida { get; set; }

    public int IdRonda { get; set; }

    public int NumeroNivel { get; set; }

    public string Nivel { get; set; } = null!;

    public string Personaje { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public int Puntuacion { get; set; }

    public int FantasmasComidos { get; set; }

    public int PacmansAtrapados { get; set; }

    public int ObjetosUsados { get; set; }

    public bool Abandono { get; set; }

    public string? BandoGanador { get; set; }

    public int? Gano { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }
}
