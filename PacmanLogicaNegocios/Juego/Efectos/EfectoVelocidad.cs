using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Items;

namespace PacmanLogicaNegocios.Juego.Efectos;

// PROVISIONAL (objetos Turbo e Impulso): quien lo usa va más rápido.
public sealed class EfectoVelocidad : IEfectoObjeto
{
    public const double Factor = 1.5;

    public string TipoEfecto
    {
        get
        {
            return "VELOCIDAD";
        }
    }

    public double MultiplicadorVelocidad(EstadoPartida partida, EfectoActivo efecto, Entidad actor)
    {
        return actor.IdPersonaje == efecto.IdPersonajeOrigen ? Factor : 1;
    }
}
