using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Mapa;

namespace PacmanLogicaNegocios.Juego.IA;

// Pinky (rosa) intenta emboscar: apunta 4 casillas por delante del Pac-Man más cercano.
public sealed class EstrategiaPinky : IEstrategiaPersecucion
{
    private const int CasillasAdelante = 4;

    public Casilla? ObtenerObjetivo(
        Fantasma fantasma, EstadoPartida partida, TimeSpan tiempoSimulacion)
    {
        var pacman = Rutas.PacManMasCercano(partida, fantasma.Posicion);

        if (pacman == null)
        {
            return null;
        }

        var (fila, columna) = Adelantar(pacman, CasillasAdelante);

        return Rutas.MasCercanaTransitable(partida.Laberinto, fila, columna);
    }

    internal static (int Fila, int Columna) Adelantar(Entidad actor, int casillas)
    {
        var (filaPaso, columnaPaso) = EstadoPartida.Desplazar(actor.Posicion, actor.Direccion);

        return (
            actor.Posicion.Fila + (filaPaso - actor.Posicion.Fila) * casillas,
            actor.Posicion.Columna + (columnaPaso - actor.Posicion.Columna) * casillas);
    }
}
