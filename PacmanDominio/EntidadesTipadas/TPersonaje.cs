namespace PacmanDominio.EntidadesTipadas;

public class TPersonaje
{
    public byte IdPersonaje { get; set; }
    public string Nombre { get; set; } = null!;
    public string Rol { get; set; } = null!;
    public string Color { get; set; } = null!;
}
