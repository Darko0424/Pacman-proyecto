using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Items;

namespace PacmanLogicaNegocios.Juego.Efectos;

// Objeto Ralentizar (fantasmas): todo el bando rival va a la mitad de velocidad.
public sealed class EfectoRalentizar : IEfectoObjeto
{
    public string TipoEfecto
    {
        get
        {
            return "RALENTIZAR";
        }
    }

    public double MultiplicadorVelocidad(EstadoPartida partida, EfectoActivo efecto, Entidad actor)
    {
        return actor.Bando != efecto.BandoOrigen ? ReglasJuego.FactorRalentizar : 1;
    }
}
