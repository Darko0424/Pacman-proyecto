using System;
using System.Collections.Generic;

namespace PacmanDominio.Entidades.Vistas;

// Una fila por usuario y partida: suma de todos los niveles que jugó
public partial class HistorialEntrada
{
    public int? IdUsuario { get; set; }

    public int IdPartida { get; set; }

    public string CodigoSala { get; set; } = null!;

    public string Modo { get; set; } = null!;

    public string Dificultad { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public int NivelesJugados { get; set; }

    public int NivelAlcanzado { get; set; }

    public int Puntuacion { get; set; }

    public int FantasmasComidos { get; set; }

    public int PacmansAtrapados { get; set; }

    public int ObjetosUsados { get; set; }

    public int NivelesGanados { get; set; }

    public bool Abandono { get; set; }
}
