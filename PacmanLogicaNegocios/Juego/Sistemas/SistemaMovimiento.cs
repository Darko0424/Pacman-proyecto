using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Mapa;

namespace PacmanLogicaNegocios.Juego.Sistemas;

// Movimiento por casillas: cada actor acumula avance según su velocidad y pasa
// a la siguiente casilla al completar 1. El rumbo solo cambia al llegar a una
// casilla (o si está detenido), como en el Pac-Man clásico.
internal static class SistemaMovimiento
{
    public static double CalcularVelocidad(ContextoMotor contexto, Entidad actor)
    {
        if (!actor.PuedeParticipar)
        {
            return 0;
        }

        double velocidad;

        if (actor is Fantasma fantasma)
        {
            if (fantasma.EstaAturdido(contexto.Tiempo))
            {
                return 0;
            }

            velocidad = ReglasJuego.VelocidadFantasma;

            // La dificultad solo cambia a los fantasmas que mueve la IA
            if (fantasma.ControlIa)
            {
                velocidad *= (double)contexto.Dificultad.MultiplicadorVelocidadIa;
            }

            if (fantasma.EstaAsustado(contexto.Tiempo))
            {
                velocidad *= ReglasJuego.FactorFantasmaAsustado;
            }
        }
        else
        {
            velocidad = ReglasJuego.VelocidadPacMan;

            if (HayBolaAdelante(contexto, actor))
            {
                velocidad *= ReglasJuego.FactorPacManComiendo;
            }
        }

        velocidad *= contexto.VelocidadNivel;

        foreach (var (efecto, implementacion) in contexto.EfectosVigentes())
        {
            velocidad *= implementacion.MultiplicadorVelocidad(contexto.Partida, efecto, actor);
        }

        return Math.Clamp(velocidad, 0, ReglasJuego.VelocidadMaxima);
    }

    // Devuelve true si el actor pasó a otra casilla
    public static bool Mover(
        ContextoMotor contexto, Entidad actor, Direccion deseada, TimeSpan transcurrido)
    {
        if (!actor.PuedeParticipar)
        {
            return false;
        }

        var laberinto = contexto.Laberinto;
        var direccion = actor.Direccion;
        var avance = actor.Avance;

        // Detenido (o frente a un muro): elige rumbo enseguida
        if (!Rutas.EstaAbierta(laberinto, actor.Posicion, direccion))
        {
            direccion = Elegir(laberinto, actor.Posicion, deseada, Direccion.Ninguna);
            avance = 0;
        }

        if (direccion == Direccion.Ninguna)
        {
            contexto.Partida.ActualizarDesplazamiento(actor.IdPersonaje, Direccion.Ninguna, 0);
            return false;
        }

        avance += CalcularVelocidad(contexto, actor) * transcurrido.TotalSeconds;
        var cambioCasilla = false;

        if (avance >= 1)
        {
            var destino = Rutas.Vecina(laberinto, actor.Posicion, direccion)!;

            cambioCasilla = contexto.Partida.MoverEntidad(
                actor.IdPersonaje, destino.Fila, destino.Columna);

            avance = Math.Min(avance - 1, 0.99);
            direccion = Elegir(laberinto, actor.Posicion, deseada, direccion);

            if (direccion == Direccion.Ninguna)
            {
                avance = 0;
            }
        }

        contexto.Partida.ActualizarDesplazamiento(actor.IdPersonaje, direccion, avance);

        return cambioCasilla;
    }

    private static bool HayBolaAdelante(ContextoMotor contexto, Entidad actor)
    {
        var siguiente = Rutas.Vecina(contexto.Laberinto, actor.Posicion, actor.Direccion);

        return siguiente != null && contexto.Laberinto.Puntos.Any(punto =>
            !punto.Consumido && Rutas.MismaCasilla(punto.Posicion, siguiente));
    }

    // Gira hacia la dirección deseada si está abierta; si no, sigue de frente;
    // si tampoco puede, se detiene.
    public static Direccion Elegir(
        Laberinto laberinto, Casilla posicion, Direccion deseada, Direccion actual)
    {
        if (Rutas.EstaAbierta(laberinto, posicion, deseada))
        {
            return deseada;
        }

        return Rutas.EstaAbierta(laberinto, posicion, actual) ? actual : Direccion.Ninguna;
    }
}
