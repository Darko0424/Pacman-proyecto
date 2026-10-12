using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Actores;

public sealed class Fantasma : Entidad
{
    // El bot no tiene usuario; cada fantasma humano debe tener uno.
    public Fantasma(byte idPersonaje, int? idUsuario, Casilla posicionInicial)
        : base(idPersonaje, idUsuario, posicionInicial)
    {
    }
}
