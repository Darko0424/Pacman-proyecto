using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Items;

public sealed class PuntoPoder : Punto
{
    // En esta etapa solo se representa su consumo para completar el mapa.
    public PuntoPoder(Casilla posicion)
        : base(posicion)
    {
    }
}
