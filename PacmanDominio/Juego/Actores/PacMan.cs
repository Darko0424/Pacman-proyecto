using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Actores;

public sealed class PacMan : Entidad
{
    public override Bando Bando
    {
        get
        {
            return Bando.PacMan;
        }
    }

    // idUsuario null = Pac-Man controlado por la IA
    public PacMan(byte idPersonaje, int? idUsuario, Casilla posicionInicial, int vidas)
        : base(idPersonaje, idUsuario, posicionInicial, vidas)
    {
        if (!CatalogoPersonajes.EsPacMan(idPersonaje))
        {
            throw new ArgumentOutOfRangeException(
                nameof(idPersonaje), "El personaje no es un Pac-Man del catálogo.");
        }
    }
}
