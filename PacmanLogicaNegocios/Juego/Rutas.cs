using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Mapa;

namespace PacmanLogicaNegocios.Juego;

// Recorridos sobre el laberinto: vecinos, direcciones y distancias reales
// (en pasos, rodeando muros) mediante búsqueda en anchura.
internal static class Rutas
{
    public const int Inalcanzable = int.MaxValue;

    // Orden clásico para desempatar: arriba, izquierda, abajo, derecha
    public static IReadOnlyList<Direccion> Direcciones { get; } = new[]
    {
        Direccion.Arriba,
        Direccion.Izquierda,
        Direccion.Abajo,
        Direccion.Derecha
    };

    public static Direccion Opuesta(Direccion direccion)
    {
        return direccion switch
        {
            Direccion.Arriba => Direccion.Abajo,
            Direccion.Abajo => Direccion.Arriba,
            Direccion.Izquierda => Direccion.Derecha,
            Direccion.Derecha => Direccion.Izquierda,
            _ => Direccion.Ninguna
        };
    }

    public static Casilla? Vecina(Laberinto laberinto, Casilla origen, Direccion direccion)
    {
        if (direccion == Direccion.Ninguna)
        {
            return null;
        }

        var (fila, columna) = EstadoPartida.Desplazar(origen, direccion);

        return laberinto.EsTransitable(fila, columna)
            ? laberinto.ObtenerCasilla(fila, columna)
            : null;
    }

    public static bool EstaAbierta(Laberinto laberinto, Casilla origen, Direccion direccion)
    {
        return Vecina(laberinto, origen, direccion) != null;
    }

    public static List<Direccion> DireccionesAbiertas(Laberinto laberinto, Casilla origen)
    {
        return Direcciones.Where(direccion => EstaAbierta(laberinto, origen, direccion)).ToList();
    }

    public static bool MismaCasilla(Casilla a, Casilla b)
    {
        return a.Fila == b.Fila && a.Columna == b.Columna;
    }

    // Pasos desde la casilla más cercana de los orígenes hasta cada casilla
    public static int[,] Distancias(Laberinto laberinto, IEnumerable<Casilla> origenes)
    {
        var distancias = new int[laberinto.Filas, laberinto.Columnas];

        for (var fila = 0; fila < laberinto.Filas; fila++)
        {
            for (var columna = 0; columna < laberinto.Columnas; columna++)
            {
                distancias[fila, columna] = Inalcanzable;
            }
        }

        var pendientes = new Queue<Casilla>();

        foreach (var origen in origenes)
        {
            if (laberinto.EsTransitable(origen.Fila, origen.Columna) &&
                distancias[origen.Fila, origen.Columna] != 0)
            {
                distancias[origen.Fila, origen.Columna] = 0;
                pendientes.Enqueue(laberinto.ObtenerCasilla(origen.Fila, origen.Columna));
            }
        }

        while (pendientes.Count > 0)
        {
            var actual = pendientes.Dequeue();
            var siguiente = distancias[actual.Fila, actual.Columna] + 1;

            foreach (var direccion in Direcciones)
            {
                var vecina = Vecina(laberinto, actual, direccion);

                if (vecina != null && distancias[vecina.Fila, vecina.Columna] == Inalcanzable)
                {
                    distancias[vecina.Fila, vecina.Columna] = siguiente;
                    pendientes.Enqueue(vecina);
                }
            }
        }

        return distancias;
    }

    public static int Distancia(int[,] distancias, Casilla casilla)
    {
        return distancias[casilla.Fila, casilla.Columna];
    }

    // Para objetivos que caen en un muro o fuera del mapa
    public static Casilla MasCercanaTransitable(Laberinto laberinto, int fila, int columna)
    {
        if (laberinto.EsTransitable(fila, columna))
        {
            return laberinto.ObtenerCasilla(fila, columna);
        }

        Casilla? mejor = null;
        var mejorDistancia = int.MaxValue;

        foreach (var casilla in laberinto.Casillas)
        {
            if (!casilla.EsTransitable)
            {
                continue;
            }

            var distancia = Math.Abs(casilla.Fila - fila) + Math.Abs(casilla.Columna - columna);

            // Empate: la de menor fila y luego menor columna
            if (distancia < mejorDistancia ||
                (distancia == mejorDistancia &&
                    (casilla.Fila < mejor!.Fila ||
                        (casilla.Fila == mejor.Fila && casilla.Columna < mejor.Columna))))
            {
                mejor = casilla;
                mejorDistancia = distancia;
            }
        }

        return mejor!;
    }

    public static PacMan? PacManMasCercano(EstadoPartida partida, Casilla desde)
    {
        return partida.Pacmans
            .Where(pacman => pacman.PuedeParticipar)
            .OrderBy(pacman =>
                Math.Abs(pacman.Posicion.Fila - desde.Fila) +
                Math.Abs(pacman.Posicion.Columna - desde.Columna))
            .ThenBy(pacman => pacman.IdPersonaje)
            .FirstOrDefault();
    }

    // Donde el actor tomará su próxima decisión: la casilla a la que está
    // llegando, o la suya si está detenido.
    public static (Casilla Casilla, Direccion Llegada) PuntoDeDecision(Laberinto laberinto, Entidad actor)
    {
        var siguiente = Vecina(laberinto, actor.Posicion, actor.Direccion);

        return siguiente == null
            ? (actor.Posicion, Direccion.Ninguna)
            : (siguiente, actor.Direccion);
    }
}
