using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Mapa;

namespace PacmanLogicaNegocios.Juego.IA;

// Inky (celeste) encierra junto con Blinky: toma el punto 2 casillas por delante
// del Pac-Man y duplica el vector que va desde Blinky hasta ese punto.
public sealed class EstrategiaInky : IEstrategiaPersecucion
{
    private const byte IdBlinky = 3;
    private const int CasillasAdelante = 2;

    public Casilla? ObtenerObjetivo(
        Fantasma fantasma, EstadoPartida partida, TimeSpan tiempoSimulacion)
    {
        var pacman = Rutas.PacManMasCercano(partida, fantasma.Posicion);

        if (pacman == null)
        {
            return null;
        }

        var (fila, columna) = EstrategiaPinky.Adelantar(pacman, CasillasAdelante);

        // Sin Blinky en juego, el vector parte del propio Inky
        var blinky = partida.Fantasmas.FirstOrDefault(otro =>
            otro.IdPersonaje == IdBlinky && otro.PuedeParticipar && otro != fantasma);
        var referencia = (blinky ?? fantasma).Posicion;

        return Rutas.MasCercanaTransitable(
            partida.Laberinto,
            fila + (fila - referencia.Fila),
            columna + (columna - referencia.Columna));
    }
}
