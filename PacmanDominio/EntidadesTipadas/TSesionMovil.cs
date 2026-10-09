namespace PacmanDominio.EntidadesTipadas;

public class TSesionMovil
{
    public int IdSesion { get; set; }
    public int IdUsuario { get; set; }
    public string TokenDispositivo { get; set; } = null!;
    public DateTime UltimoAcceso { get; set; }
}
