using PacmanDominio.Juego;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Items;

namespace PacmanLogicaNegocios.Juego.Efectos;

// Objeto Trampa de hielo (fantasmas): deja una trampa en su casilla que dura lo
// que diga el catálogo. Quién la pisa lo resuelve SistemaObjetos.ActivarTrampas.
public sealed class EfectoTrampa : IEfectoObjeto
{
    public string TipoEfecto
    {
        get
        {
            return "TRAMPA";
        }
    }

    public void Aplicar(EstadoPartida partida, EfectoActivo efecto)
    {
        var actor = partida.ObtenerActor(efecto.IdPersonajeOrigen);

        // Si ya hay una trampa en esa casilla, el objeto se gasta sin poner otra
        partida.ColocarTrampa(new Trampa(efecto.IdEfecto, actor.Posicion, actor.IdPersonaje, efecto.Fin));
    }

    // Nadie la pisó a tiempo
    public void Retirar(EstadoPartida partida, EfectoActivo efecto)
    {
        partida.QuitarTrampa(efecto.IdEfecto);
    }
}
