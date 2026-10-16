using PacmanDominio.Juego;
using PacmanDominio.Juego.Items;

namespace PacmanLogicaNegocios.Juego.Sistemas;

// Bolas comidas, bolas de poder y bonificación del bando ganador.
// Los puntos de cada captura se suman en SistemaColisiones.
internal static class SistemaPuntaje
{
    public static void ComerBolas(ContextoMotor contexto)
    {
        var partida = contexto.Partida;

        foreach (var pacman in partida.Pacmans)
        {
            if (partida.Terminada)
            {
                return;
            }

            if (!pacman.PuedeParticipar)
            {
                continue;
            }

            var punto = partida.Laberinto.Puntos.FirstOrDefault(item =>
                !item.Consumido && Rutas.MismaCasilla(item.Posicion, pacman.Posicion));

            if (punto == null)
            {
                continue;
            }

            var esDePoder = punto is PuntoPoder;

            // Los puntos van antes: la última bola termina el nivel
            partida.SumarPuntos(
                pacman.IdPersonaje,
                esDePoder ? ReglasJuego.PuntosBolaPoder : ReglasJuego.PuntosBola);

            if (partida.ConsumirPunto(pacman.IdPersonaje) == null)
            {
                continue;
            }

            contexto.Registrar(new EventoJuego(
                esDePoder ? TipoEventoJuego.BolaPoderComida : TipoEventoJuego.BolaComida,
                pacman.IdPersonaje, null, punto.Posicion.Fila, punto.Posicion.Columna));

            if (esDePoder)
            {
                // La duración del susto depende de la dificultad
                var hasta = contexto.Tiempo +
                    TimeSpan.FromSeconds(contexto.Dificultad.DuracionAsustadoSeg);

                foreach (var fantasma in partida.Fantasmas.Where(fantasma => fantasma.PuedeParticipar))
                {
                    partida.AsustarFantasma(fantasma.IdPersonaje, hasta);
                }
            }
        }
    }

    public static void CerrarNivel(ContextoMotor contexto)
    {
        if (contexto.Partida.OtorgarBonificacion(ReglasJuego.PuntosNivelGanado))
        {
            contexto.Registrar(new EventoJuego(TipoEventoJuego.NivelTerminado));
        }
    }
}
