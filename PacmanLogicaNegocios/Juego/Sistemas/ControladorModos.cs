using PacmanDominio.Juego.Mapa;

namespace PacmanLogicaNegocios.Juego.Sistemas;

internal enum ModoFantasmas
{
    // Cada fantasma de la IA va a su esquina
    Dispersion = 1,
    // Cada fantasma de la IA persigue según su estrategia
    Persecucion = 2
}

internal static class ControladorModos
{
    public static ModoFantasmas ObtenerModo(TimeSpan tiempoSimulacion)
    {
        var acumulado = TimeSpan.Zero;
        var modo = ModoFantasmas.Dispersion;

        foreach (var duracion in ReglasJuego.CicloModos)
        {
            acumulado += duracion;

            if (tiempoSimulacion < acumulado)
            {
                return modo;
            }

            modo = modo == ModoFantasmas.Dispersion
                ? ModoFantasmas.Persecucion
                : ModoFantasmas.Dispersion;
        }

        return ModoFantasmas.Persecucion;
    }

    // Blinky arriba a la derecha, Pinky arriba a la izquierda, Inky abajo a la derecha
    public static Casilla ObtenerEsquina(Laberinto laberinto, byte idPersonaje)
    {
        var (fila, columna) = idPersonaje switch
        {
            3 => (0, laberinto.Columnas - 1),
            4 => (0, 0),
            5 => (laberinto.Filas - 1, laberinto.Columnas - 1),
            _ => (laberinto.Filas - 1, 0)
        };

        return Rutas.MasCercanaTransitable(laberinto, fila, columna);
    }
}
