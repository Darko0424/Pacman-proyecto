using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Mapa;
using PacmanLogicaNegocios.Juego.IA;
using PacmanLogicaNegocios.Juego.Sistemas;

namespace PacmanLogicaNegocios.Juego.Controladores;

// Mueve a los personajes de la IA y a los de jugadores desconectados o que se fueron.
// Decide para la casilla a la que el actor está llegando, que es donde podrá girar.
internal sealed class ControladorIA : IControlador
{
    private const long Evitar = 1_000_000;

    private static readonly IReadOnlyDictionary<byte, IEstrategiaPersecucion> Estrategias =
        new Dictionary<byte, IEstrategiaPersecucion>
        {
            [3] = new EstrategiaBlinky(),
            [4] = new EstrategiaPinky(),
            [5] = new EstrategiaInky()
        };

    private readonly ContextoMotor _contexto;

    public ControladorIA(ContextoMotor contexto)
    {
        _contexto = contexto;
    }

    public DecisionControl ObtenerDecision(
        Entidad actor, EstadoPartida partida, TimeSpan tiempoSimulacion)
    {
        if (!actor.PuedeParticipar)
        {
            return new DecisionControl(Direccion.Ninguna, false);
        }

        var direccion = actor is Fantasma fantasma
            ? DecidirFantasma(fantasma, partida, tiempoSimulacion)
            : DecidirPacMan(actor, partida, tiempoSimulacion);

        return new DecisionControl(direccion, QuiereUsarObjeto(actor, partida));
    }

    private Direccion DecidirFantasma(Fantasma fantasma, EstadoPartida partida, TimeSpan tiempo)
    {
        if (fantasma.EstaAturdido(tiempo))
        {
            return Direccion.Ninguna;
        }

        var laberinto = partida.Laberinto;
        var (punto, llegada) = Rutas.PuntoDeDecision(laberinto, fantasma);

        // Como en el clásico, no da media vuelta salvo que no tenga otra salida
        var opciones = Rutas.DireccionesAbiertas(laberinto, punto)
            .Where(direccion => direccion != Rutas.Opuesta(llegada))
            .ToList();

        if (opciones.Count == 0)
        {
            opciones = Rutas.DireccionesAbiertas(laberinto, punto);
        }

        if (opciones.Count == 0)
        {
            return Direccion.Ninguna;
        }

        // Asustado: se aleja de los Pac-Man
        if (fantasma.EstaAsustado(tiempo))
        {
            var pacmans = partida.Pacmans
                .Where(pacman => pacman.PuedeParticipar)
                .Select(pacman => pacman.Posicion)
                .ToList();

            if (pacmans.Count == 0)
            {
                return opciones[_contexto.Azar.Next(opciones.Count)];
            }

            var lejania = Rutas.Distancias(laberinto, pacmans);

            return MejorOpcion(laberinto, punto, opciones, Direccion.Ninguna,
                vecina => -(long)Rutas.Distancia(lejania, vecina));
        }

        Casilla? objetivo = null;

        if (ControladorModos.ObtenerModo(tiempo) == ModoFantasmas.Persecucion &&
            Estrategias.TryGetValue(fantasma.IdPersonaje, out var estrategia))
        {
            objetivo = estrategia.ObtenerObjetivo(fantasma, partida, tiempo);
        }

        objetivo ??= ControladorModos.ObtenerEsquina(laberinto, fantasma.IdPersonaje);

        var distancias = Rutas.Distancias(laberinto, new[] { objetivo });

        // Evita pisar la casilla de otro fantasma: chocar es una explosión
        var ocupadas = partida.Fantasmas
            .Where(otro => otro != fantasma && otro.PuedeParticipar)
            .Select(otro => (otro.Posicion.Fila, otro.Posicion.Columna))
            .ToHashSet();

        return MejorOpcion(laberinto, punto, opciones, Direccion.Ninguna, vecina =>
            Rutas.Distancia(distancias, vecina) +
            (ocupadas.Contains((vecina.Fila, vecina.Columna))
                ? ReglasJuego.PenalizacionCasillaOcupada
                : 0L));
    }

    private Direccion DecidirPacMan(Entidad pacman, EstadoPartida partida, TimeSpan tiempo)
    {
        var laberinto = partida.Laberinto;
        var (punto, llegada) = Rutas.PuntoDeDecision(laberinto, pacman);
        var opciones = Rutas.DireccionesAbiertas(laberinto, punto);

        if (opciones.Count == 0)
        {
            return Direccion.Ninguna;
        }

        var protegido = _contexto.EstaProtegido(pacman) || pacman.EsInvencible(tiempo);
        var fantasmas = partida.Fantasmas.Where(fantasma => fantasma.PuedeParticipar).ToList();

        var peligrosos = protegido
            ? new List<Casilla>()
            : fantasmas
                .Where(fantasma => !fantasma.EstaAsustado(tiempo) && !fantasma.EstaAturdido(tiempo))
                .Select(fantasma => fantasma.Posicion)
                .ToList();
        var presas = fantasmas
            .Where(fantasma => fantasma.EstaAsustado(tiempo) && !fantasma.EsInvencible(tiempo) &&
                !_contexto.EstaProtegido(fantasma))
            .Select(fantasma => fantasma.Posicion)
            .ToList();

        var peligro = peligrosos.Count > 0 ? Rutas.Distancias(laberinto, peligrosos) : null;
        long Peligro(Casilla casilla) =>
            peligro == null ? Rutas.Inalcanzable : Rutas.Distancia(peligro, casilla);

        var bolas = laberinto.Puntos
            .Where(bola => !bola.Consumido)
            .Select(bola => bola.Posicion)
            .ToList();
        var cercania = bolas.Count > 0 ? Rutas.Distancias(laberinto, bolas) : null;
        long Bolas(Casilla casilla) =>
            cercania == null ? 0 : Math.Min(Rutas.Distancia(cercania, casilla), 999);

        // 1. Un fantasma peligroso cerca: huye, y de paso come si puede
        if (Peligro(punto) <= ReglasJuego.DistanciaPeligroPacMan)
        {
            return MejorOpcion(laberinto, punto, opciones, llegada,
                vecina => -Peligro(vecina) * 1000 + Bolas(vecina));
        }

        // 2. Fantasmas asustados al alcance: los persigue
        if (presas.Count > 0)
        {
            var hastaPresa = Rutas.Distancias(laberinto, presas);

            if (Rutas.Distancia(hastaPresa, punto) <= ReglasJuego.DistanciaPersecucionPresa)
            {
                return MejorOpcion(laberinto, punto, opciones, llegada, vecina =>
                    Rutas.Distancia(hastaPresa, vecina) + (Peligro(vecina) <= 1 ? Evitar : 0));
            }
        }

        // 3. Va por la bola más cercana sin acercarse demasiado a los fantasmas
        return MejorOpcion(laberinto, punto, opciones, llegada, vecina =>
            Bolas(vecina) + (Peligro(vecina) <= 2 ? Evitar : 0));
    }

    // La IA usa su objeto cuando tiene un rival cerca
    private static bool QuiereUsarObjeto(Entidad actor, EstadoPartida partida)
    {
        if (!actor.IdObjetoGuardado.HasValue)
        {
            return false;
        }

        var rivales = partida.Actores
            .Where(otro => otro.Bando != actor.Bando && otro.PuedeParticipar)
            .ToList();

        if (rivales.Count == 0)
        {
            return false;
        }

        var distancias = Rutas.Distancias(partida.Laberinto, new[] { actor.Posicion });

        return rivales.Any(rival =>
            Rutas.Distancia(distancias, rival.Posicion) <= ReglasJuego.DistanciaUsoObjeto);
    }

    // La de menor costo; ante un empate gana la preferida y luego el orden clásico
    private static Direccion MejorOpcion(
        Laberinto laberinto,
        Casilla punto,
        IReadOnlyList<Direccion> opciones,
        Direccion preferida,
        Func<Casilla, long> costo)
    {
        var mejor = Direccion.Ninguna;
        var mejorCosto = long.MaxValue;

        foreach (var direccion in opciones.OrderBy(direccion => direccion == preferida ? 0 : 1))
        {
            var valor = costo(Rutas.Vecina(laberinto, punto, direccion)!);

            if (valor < mejorCosto)
            {
                mejor = direccion;
                mejorCosto = valor;
            }
        }

        return mejor;
    }
}
