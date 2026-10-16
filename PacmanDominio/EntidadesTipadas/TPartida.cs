namespace PacmanDominio.EntidadesTipadas;

public class TPartida
{
    public int IdPartida { get; set; }
    public string CodigoSala { get; set; } = null!;
    public string Modo { get; set; } = null!;
    public byte IdDificultad { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}
