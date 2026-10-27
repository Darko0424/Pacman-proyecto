using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Mapa;

namespace PacmanLogicaNegocios.Juego.Sistemas;

// Quien perdió una vida reaparece, tras una demora, en el punto de reaparición
// libre más alejado de sus rivales.
internal static class SistemaReaparicion
{
    public static void Procesar(ContextoMotor contexto)
    {
        var reservadas = new HashSet<(int Fila, int Columna)>();

        foreach (var actor in contexto.Partida.Actores.ToList())
        {
            if (!actor.ReaparicionPendiente || actor.Eliminado)
            {
                continue;
            }

            var muerte = contexto.Muertes.GetValueOrDefault(actor.IdPersonaje, TimeSpan.Zero);

            if (contexto.Tiempo - muerte <
                ReglasJuego.DemoraReaparicionDe(actor, contexto.Partida.Configuracion))
            {
                continue;
            }

            var destino = ElegirPuntoSeguro(contexto, actor, reservadas);

            if (contexto.Partida.Reaparecer(actor.IdPersonaje, destino.Fila, destino.Columna))
            {
                reservadas.Add((destino.Fila, destino.Columna));
                contexto.Registrar(new EventoJuego(
                    TipoEventoJuego.Reaparicion, actor.IdPersonaje, null,
                    destino.Fila, destino.Columna));
            }
        }
    }

    public static Casilla ElegirPuntoSeguro(
        ContextoMotor contexto, Entidad actor, ISet<(int Fila, int Columna)> reservadas)
    {
        var actores = contexto.Partida.Actores.Where(otro => otro.PuedeParticipar).ToList();
        var rivales = actores
            .Where(otro => otro.Bando != actor.Bando)
            .Select(otro => otro.Posicion)
            .ToList();
        var ocupadas = actores
            .Where(otro => otro.IdPersonaje != actor.IdPersonaje)
            .Select(otro => (otro.Posicion.Fila, otro.Posicion.Columna))
            .Concat(reservadas)
            .ToHashSet();

        var distancias = rivales.Count > 0 ? Rutas.Distancias(contexto.Laberinto, rivales) : null;
        var candidatos = contexto.Laberinto.PuntosReaparicion
            .Where(punto => !ocupadas.Contains((punto.Fila, punto.Columna)))
            .ToList();

        if (candidatos.Count == 0)
        {
            candidatos = contexto.Laberinto.PuntosReaparicion.ToList();
        }

        // Entre los igual de alejados se elige al azar
        return candidatos
            .Select(punto => (Punto: punto, Azar: contexto.Azar.Next()))
            .OrderByDescending(candidato =>
                distancias == null ? 0 : Rutas.Distancia(distancias, candidato.Punto))
            .ThenBy(candidato => candidato.Azar)
            .First()
            .Punto;
    }
}
