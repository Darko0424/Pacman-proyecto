namespace PacmanDominio.EntidadesTipadas;

public class TObjeto
{
    public byte IdObjeto { get; set; }
    public string Nombre { get; set; } = null!;
    public string Bando { get; set; } = null!;
    public string Efecto { get; set; } = null!;
    public byte DuracionSeg { get; set; }
}
