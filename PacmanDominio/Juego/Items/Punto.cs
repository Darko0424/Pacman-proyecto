using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Items;

public class Punto
{
    public Casilla Posicion { get; }
    public bool Consumido { get; private set; }

    public Punto(Casilla posicion)
    {
        ArgumentNullException.ThrowIfNull(posicion);

        if (!posicion.EsTransitable)
        {
            throw new ArgumentException(
                "La bola debe estar en una casilla transitable.",
                nameof(posicion));
        }

        Posicion = posicion;
    }

    internal bool Consumir()
    {
        if (Consumido)
        {
            return false;
        }

        Consumido = true;

        return true;
    }
}
