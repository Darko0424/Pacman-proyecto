using PacmanDominio.Juego;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Items;

namespace PacmanLogicaNegocios.Juego.Efectos;

// Objeto Teletransporte (fantasmas): salta hacia adelante en línea recta por su
// pasillo, hasta ReglasJuego.DistanciaTeletransporte casillas, sin atravesar muros.
// Es instantáneo: la duración del catálogo solo lo deja en la lista de efectos.
public sealed class EfectoTeletransporte : IEfectoObjeto
{
    public string TipoEfecto
    {
        get
        {
            return "TELETRANSPORTE";
        }
    }

    public void Aplicar(EstadoPartida partida, EfectoActivo efecto)
    {
        var actor = partida.ObtenerActor(efecto.IdPersonajeOrigen);
        var direccion = actor.Direccion;

        // Detenido no hay "adelante": el objeto se gasta sin moverlo
        if (direccion == Direccion.Ninguna)
        {
            return;
        }

        var destino = actor.Posicion;

        for (var paso = 0; paso < ReglasJuego.DistanciaTeletransporte; paso++)
        {
            var (fila, columna) = EstadoPartida.Desplazar(destino, direccion);

            if (!partida.Laberinto.EsTransitable(fila, columna))
            {
                break;
            }

            destino = partida.Laberinto.ObtenerCasilla(fila, columna);
        }

        if (Rutas.MismaCasilla(destino, actor.Posicion) ||
            !partida.MoverEntidad(actor.IdPersonaje, destino.Fila, destino.Columna))
        {
            return;
        }

        // Llega al centro de la casilla; sigue de frente si puede
        var (siguienteFila, siguienteColumna) = EstadoPartida.Desplazar(destino, direccion);
        var sigue = partida.Laberinto.EsTransitable(siguienteFila, siguienteColumna);

        partida.ActualizarDesplazamiento(actor.IdPersonaje, sigue ? direccion : Direccion.Ninguna, 0);
    }
}
