using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Items;

namespace PacmanLogicaNegocios.Juego.Efectos;

// PROVISIONAL (objeto Hielo): todo el bando rival queda inmóvil.
public sealed class EfectoCongelar : IEfectoObjeto
{
    public string TipoEfecto
    {
        get
        {
            return "CONGELAR";
        }
    }

    public double MultiplicadorVelocidad(EstadoPartida partida, EfectoActivo efecto, Entidad actor)
    {
        return actor.Bando != efecto.BandoOrigen ? 0 : 1;
    }
}
