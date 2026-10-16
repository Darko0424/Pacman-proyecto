namespace PacmanDominio.EntidadesTipadas;

public class TParticipanteRonda
{
    public int IdParticipante { get; set; }
    public int IdRonda { get; set; }
    public int? IdUsuario { get; set; }
    public byte IdPersonaje { get; set; }
    public bool EsIa { get; set; }
    public int Puntuacion { get; set; }
    public int FantasmasComidos { get; set; }
    public int PacmansAtrapados { get; set; }
    public int ObjetosUsados { get; set; }
    public bool Abandono { get; set; }
}
