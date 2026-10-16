using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Items;

// Caja que aparece en uno de los puntos de cajas del laberinto. No guarda el
// objeto: se sortea al recogerla, entre los del bando de quien la recoge.
public sealed class CajaObjeto
{
    public int IdCaja { get; }
    public Casilla Posicion { get; }
    public bool Recogida { get; private set; }
    public byte? IdPersonajeRecolector { get; private set; }

    public bool Disponible
    {
        get
        {
            return !Recogida;
        }
    }

    public CajaObjeto(int idCaja, Casilla posicion)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idCaja);
        ArgumentNullException.ThrowIfNull(posicion);

        if (!posicion.EsTransitable)
        {
            throw new ArgumentException(
                "La caja debe estar en una casilla transitable.", nameof(posicion));
        }

        IdCaja = idCaja;
        Posicion = posicion;
    }

    internal bool Recoger(byte idPersonaje)
    {
        if (Recogida)
        {
            return false;
        }

        Recogida = true;
        IdPersonajeRecolector = idPersonaje;

        return true;
    }
}
