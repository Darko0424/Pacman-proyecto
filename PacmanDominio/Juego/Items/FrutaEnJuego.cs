using PacmanDominio.Entidades;
using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Items;

public sealed class FrutaEnJuego
{
    public int IdFrutaEnJuego { get; }
    public byte IdFruta { get; }
    public string Nombre { get; }
    public string Efecto { get; }
    public byte DuracionSeg { get; }
    public int Puntos { get; }
    public Casilla Posicion { get; }
    public bool Consumida { get; private set; }
    public byte? IdPersonajeConsumidor { get; private set; }
    public TimeSpan? TiempoConsumo { get; private set; }

    public bool Disponible
    {
        get
        {
            return !Consumida;
        }
    }

    public FrutaEnJuego(int idFrutaEnJuego, Fruta definicion, Casilla posicion)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idFrutaEnJuego);
        ArgumentNullException.ThrowIfNull(definicion);
        ArgumentNullException.ThrowIfNull(posicion);
        ArgumentOutOfRangeException.ThrowIfZero(definicion.IdFruta);
        ArgumentException.ThrowIfNullOrWhiteSpace(definicion.Nombre);
        ArgumentOutOfRangeException.ThrowIfZero(definicion.DuracionSeg);
        ArgumentOutOfRangeException.ThrowIfNegative(definicion.Puntos);

        if (!posicion.EsTransitable)
        {
            throw new ArgumentException("La fruta debe estar en una casilla transitable.", nameof(posicion));
        }

        ValidarEfecto(definicion.Efecto);

        IdFrutaEnJuego = idFrutaEnJuego;
        IdFruta = definicion.IdFruta;
        Nombre = definicion.Nombre;
        Efecto = definicion.Efecto;
        DuracionSeg = definicion.DuracionSeg;
        Puntos = definicion.Puntos;
        Posicion = posicion;
    }

    internal bool Consumir(byte idPersonaje, TimeSpan tiempoSimulacion)
    {
        if (Consumida)
        {
            return false;
        }

        Consumida = true;
        IdPersonajeConsumidor = idPersonaje;
        TiempoConsumo = tiempoSimulacion;

        return true;
    }

    private static void ValidarEfecto(string efecto)
    {
        if (efecto != "VELOCIDAD" && efecto != "CONGELAR_FANTASMAS" &&
            efecto != "INVISIBILIDAD" && efecto != "DOBLE_PUNTAJE")
        {
            throw new ArgumentException("El efecto de fruta no pertenece al catálogo.", nameof(efecto));
        }
    }
}
