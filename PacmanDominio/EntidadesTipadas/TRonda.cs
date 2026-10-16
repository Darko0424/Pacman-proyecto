namespace PacmanDominio.EntidadesTipadas;

public class TRonda
{
    public int IdRonda { get; set; }
    public int IdPartida { get; set; }
    public int IdNivel { get; set; }
    public string? BandoGanador { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}
