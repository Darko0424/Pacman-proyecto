namespace PacmanDominio.EntidadesTipadas;

public class TPartida
{
    public int IdPartida { get; set; }
    public string? CodigoSala { get; set; }
    public string Modo { get; set; } = null!;
    public byte IdDificultad { get; set; }
    public int IdNivelAlcanzado { get; set; }
    public string Estado { get; set; } = null!;
    public string? BandoGanador { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}
