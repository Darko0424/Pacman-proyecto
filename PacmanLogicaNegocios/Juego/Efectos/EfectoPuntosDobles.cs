using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Items;

namespace PacmanLogicaNegocios.Juego.Efectos;

// Objeto Puntos dobles (Pac-Man): quien lo usa gana el doble por bolas y fantasmas.
public sealed class EfectoPuntosDobles : IEfectoObjeto
{
    public string TipoEfecto
    {
        get
        {
            return "PUNTOS_DOBLES";
        }
    }

    public int MultiplicadorPuntos(EstadoPartida partida, EfectoActivo efecto, Entidad actor)
    {
        return actor.IdPersonaje == efecto.IdPersonajeOrigen ? ReglasJuego.MultiplicadorPuntosDobles : 1;
    }
}
