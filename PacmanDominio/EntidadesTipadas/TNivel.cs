namespace PacmanDominio.EntidadesTipadas;

public class TNivel
{
    public int IdNivel { get; set; }
    public int Numero { get; set; }
    public string Nombre { get; set; } = null!;
    public string LaberintoJson { get; set; } = null!;
    public decimal VelocidadBase { get; set; }
}
