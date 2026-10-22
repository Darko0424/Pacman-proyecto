using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Items;

namespace PacmanLogicaNegocios.Juego.Efectos;

// Objeto Escudo (Pac-Man): quien lo usa no puede ser atrapado.
public sealed class EfectoEscudo : IEfectoObjeto
{
    public string TipoEfecto
    {
        get
        {
            return "ESCUDO";
        }
    }

    public bool Protege(EstadoPartida partida, EfectoActivo efecto, Entidad actor)
    {
        return actor.IdPersonaje == efecto.IdPersonajeOrigen;
    }
}
