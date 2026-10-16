using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Mapa;

namespace PacmanLogicaNegocios.Juego.Sistemas;

// Dos actores chocan si terminan en la misma casilla o si se cruzaron
// (intercambiaron casillas) durante el tick.
internal static class SistemaColisiones
{
    public static void Resolver(ContextoMotor contexto, IReadOnlyDictionary<byte, Casilla> previas)
    {
        // Activable desde la configuración de la sala (por defecto rebotan)
        if (contexto.Partida.Configuracion.ColisionEntrePacMan)
        {
            ResolverEntrePacMan(contexto, previas);
        }

        ResolverEntreFantasmas(contexto, previas);
        ResolverPacManFantasma(contexto, previas);
    }

    private static bool Chocan(Entidad a, Entidad b, IReadOnlyDictionary<byte, Casilla> previas)
    {
        if (Rutas.MismaCasilla(a.Posicion, b.Posicion))
        {
            return true;
        }

        return previas.TryGetValue(a.IdPersonaje, out var previaA) &&
            previas.TryGetValue(b.IdPersonaje, out var previaB) &&
            Rutas.MismaCasilla(previaA, b.Posicion) &&
            Rutas.MismaCasilla(previaB, a.Posicion);
    }

    // Rebote: cada uno regresa a su casilla anterior y da la vuelta
    private static void ResolverEntrePacMan(
        ContextoMotor contexto, IReadOnlyDictionary<byte, Casilla> previas)
    {
        var pacmans = contexto.Partida.Pacmans.Where(pacman => pacman.PuedeParticipar).ToList();

        for (var i = 0; i < pacmans.Count; i++)
        {
            for (var j = i + 1; j < pacmans.Count; j++)
            {
                var a = pacmans[i];
                var b = pacmans[j];

                if (!Chocan(a, b, previas))
                {
                    continue;
                }

                contexto.Registrar(new EventoJuego(
                    TipoEventoJuego.Rebote, a.IdPersonaje, b.IdPersonaje,
                    b.Posicion.Fila, b.Posicion.Columna));

                Rebotar(contexto, a, previas);
                Rebotar(contexto, b, previas);
            }
        }
    }

    private static void Rebotar(
        ContextoMotor contexto, Entidad actor, IReadOnlyDictionary<byte, Casilla> previas)
    {
        if (previas.TryGetValue(actor.IdPersonaje, out var previa) &&
            !Rutas.MismaCasilla(previa, actor.Posicion))
        {
            contexto.Partida.MoverEntidad(actor.IdPersonaje, previa.Fila, previa.Columna);
        }

        var vuelta = Rutas.Opuesta(actor.Direccion);

        if (!Rutas.EstaAbierta(contexto.Laberinto, actor.Posicion, vuelta))
        {
            vuelta = Direccion.Ninguna;
        }

        contexto.Partida.ActualizarDesplazamiento(actor.IdPersonaje, vuelta, 0);
    }

    // Explosión: quedan aturdidos sin perder vidas y vuelven a puntos de reaparición
    // distintos, así no siguen chocando entre ellos.
    private static void ResolverEntreFantasmas(
        ContextoMotor contexto, IReadOnlyDictionary<byte, Casilla> previas)
    {
        var tiempo = contexto.Tiempo;
        var fantasmas = contexto.Partida.Fantasmas
            .Where(fantasma => fantasma.PuedeParticipar && !fantasma.EstaAturdido(tiempo))
            .ToList();
        var explotados = new List<Fantasma>();

        for (var i = 0; i < fantasmas.Count; i++)
        {
            for (var j = i + 1; j < fantasmas.Count; j++)
            {
                var a = fantasmas[i];
                var b = fantasmas[j];

                if (explotados.Contains(a) || explotados.Contains(b) || !Chocan(a, b, previas))
                {
                    continue;
                }

                contexto.Registrar(new EventoJuego(
                    TipoEventoJuego.Explosion, a.IdPersonaje, b.IdPersonaje,
                    b.Posicion.Fila, b.Posicion.Columna));

                explotados.Add(a);
                explotados.Add(b);
            }
        }

        var reservadas = new HashSet<(int Fila, int Columna)>();

        foreach (var fantasma in explotados)
        {
            var destino = SistemaReaparicion.ElegirPuntoSeguro(contexto, fantasma, reservadas);
            reservadas.Add((destino.Fila, destino.Columna));

            contexto.Partida.MoverEntidad(fantasma.IdPersonaje, destino.Fila, destino.Columna);
            contexto.Partida.ActualizarDesplazamiento(fantasma.IdPersonaje, Direccion.Ninguna, 0);
            contexto.Partida.AturdirFantasma(
                fantasma.IdPersonaje, tiempo + ReglasJuego.DuracionAturdimiento);
        }
    }

    private static void ResolverPacManFantasma(
        ContextoMotor contexto, IReadOnlyDictionary<byte, Casilla> previas)
    {
        var partida = contexto.Partida;
        var tiempo = contexto.Tiempo;

        foreach (var pacman in partida.Pacmans)
        {
            foreach (var fantasma in partida.Fantasmas)
            {
                if (partida.Terminada)
                {
                    return;
                }

                // Si ya lo atraparon en este tick, sigue con el otro Pac-Man
                if (!pacman.PuedeParticipar)
                {
                    break;
                }

                if (!fantasma.PuedeParticipar || !Chocan(pacman, fantasma, previas))
                {
                    continue;
                }

                if (fantasma.EstaAsustado(tiempo))
                {
                    if (!contexto.EstaProtegido(fantasma) && !fantasma.EsInvencible(tiempo))
                    {
                        // Los puntos van antes: perder la última vida puede terminar el nivel
                        partida.SumarPuntos(pacman.IdPersonaje, ReglasJuego.PuntosFantasmaComido);
                        partida.RegistrarFantasmaComido(pacman.IdPersonaje);
                        contexto.Registrar(new EventoJuego(
                            TipoEventoJuego.FantasmaComido, pacman.IdPersonaje, fantasma.IdPersonaje,
                            fantasma.Posicion.Fila, fantasma.Posicion.Columna));
                        PerderVida(contexto, fantasma);
                    }

                    continue;
                }

                // Un fantasma aturdido no atrapa; el escudo y la reaparición protegen
                if (fantasma.EstaAturdido(tiempo) || contexto.EstaProtegido(pacman) ||
                    pacman.EsInvencible(tiempo))
                {
                    continue;
                }

                partida.SumarPuntos(fantasma.IdPersonaje, ReglasJuego.PuntosPacManAtrapado);
                partida.RegistrarPacManAtrapado(fantasma.IdPersonaje);
                contexto.Registrar(new EventoJuego(
                    TipoEventoJuego.PacManAtrapado, fantasma.IdPersonaje, pacman.IdPersonaje,
                    pacman.Posicion.Fila, pacman.Posicion.Columna));
                PerderVida(contexto, pacman);
            }
        }
    }

    private static void PerderVida(ContextoMotor contexto, Entidad actor)
    {
        if (contexto.Partida.PerderVida(actor.IdPersonaje))
        {
            contexto.Muertes[actor.IdPersonaje] = contexto.Tiempo;
            contexto.Partida.ActualizarDesplazamiento(actor.IdPersonaje, Direccion.Ninguna, 0);
        }
    }
}
