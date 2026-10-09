namespace PacmanDominio.EntidadesTipadas;

public class TParticipantePartida
{
    public int IdParticipante { get; set; }
    public int IdPartida { get; set; }
    public int? IdUsuario { get; set; }
    public byte IdPersonaje { get; set; }
    public bool EsIa { get; set; }
    public int Puntuacion { get; set; }
    public int FantasmasComidos { get; set; }
    public int PacmansAtrapados { get; set; }
    public int FrutasComidas { get; set; }
    public bool Abandono { get; set; }
}
