using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Interfaces;

public interface IEstrategiaPersecucion
{
    // Devuelve el objetivo de persecución, o null cuando no hay uno.
    // La búsqueda del camino corresponde al controlador o sistema de movimiento.
    Casilla? ObtenerObjetivo(
        Fantasma fantasma, EstadoPartida partida, TimeSpan tiempoSimulacion);
}
