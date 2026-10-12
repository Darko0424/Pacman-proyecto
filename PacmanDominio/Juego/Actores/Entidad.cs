using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Actores;

public abstract class Entidad
{
    public byte IdPersonaje { get; }
    public int? IdUsuario { get; }
    public Casilla Posicion { get; private set; }

    public bool EsIa
    {
        get
        {
            return IdUsuario == null;
        }
    }

    protected Entidad(byte idPersonaje, int? idUsuario, Casilla posicionInicial)
    {
        ArgumentOutOfRangeException.ThrowIfZero(idPersonaje);
        ArgumentNullException.ThrowIfNull(posicionInicial);

        if (idUsuario.HasValue && idUsuario.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(idUsuario));
        }

        if (!posicionInicial.EsTransitable)
        {
            throw new ArgumentException(
                "La posición inicial debe ser transitable.",
                nameof(posicionInicial));
        }

        IdPersonaje = idPersonaje;
        IdUsuario = idUsuario;
        Posicion = posicionInicial;
    }

    internal void CambiarPosicion(Casilla destino)
    {
        Posicion = destino;
    }
}
