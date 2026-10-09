namespace PacmanDominio.EntidadesTipadas;

public class TDificultad
{
    public byte IdDificultad { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal MultiplicadorVelocidadIa { get; set; }
    public byte DuracionAsustadoSeg { get; set; }
}
