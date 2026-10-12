using PacmanDominio.Juego.Actores;

namespace PacmanDominio.Juego.Interfaces;

// Valores estables para entradas humanas y decisiones de los bots.
public enum Direccion
{
    Ninguna = 0,
    Arriba = 1,
    Abajo = 2,
    Izquierda = 3,
    Derecha = 4
}

public interface IControlador
{
    // Devuelve una intención; el sistema de movimiento valida el recorrido.
    Direccion ObtenerDireccion(
        Entidad actor, EstadoPartida partida, TimeSpan tiempoSimulacion);
}
