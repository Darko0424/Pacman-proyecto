namespace PacmanDominio.EntidadesTipadas;

public class TFruta
{
    public byte IdFruta { get; set; }
    public string Nombre { get; set; } = null!;
    public string Efecto { get; set; } = null!;
    public byte DuracionSeg { get; set; }
    public int Puntos { get; set; }
}
