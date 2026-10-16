using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Mapa;

namespace PacmanLogicaNegocios.Juego.IA;

// Blinky (rojo) persigue directamente al Pac-Man más cercano.
public sealed class EstrategiaBlinky : IEstrategiaPersecucion
{
    public Casilla? ObtenerObjetivo(
        Fantasma fantasma, EstadoPartida partida, TimeSpan tiempoSimulacion)
    {
        return Rutas.PacManMasCercano(partida, fantasma.Posicion)?.Posicion;
    }
}
