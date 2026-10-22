using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Items;

// Trampa de hielo que deja un fantasma en su casilla. El primer Pac-Man que la
// pisa queda congelado y la trampa desaparece; si nadie la pisa, vence en Fin.
public sealed class Trampa
{
    // El mismo identificador que el EfectoActivo que la creó
    public int IdTrampa { get; }
    public Casilla Posicion { get; }
    public byte IdPersonajeOrigen { get; }
    public TimeSpan Fin { get; }

    public Trampa(int idTrampa, Casilla posicion, byte idPersonajeOrigen, TimeSpan fin)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idTrampa);
        ArgumentNullException.ThrowIfNull(posicion);
        ArgumentOutOfRangeException.ThrowIfZero(idPersonajeOrigen);

        if (!posicion.EsTransitable)
        {
            throw new ArgumentException(
                "La trampa debe estar en una casilla transitable.", nameof(posicion));
        }

        IdTrampa = idTrampa;
        Posicion = posicion;
        IdPersonajeOrigen = idPersonajeOrigen;
        Fin = fin;
    }
}
