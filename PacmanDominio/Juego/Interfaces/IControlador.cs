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

// Intención de un jugador o de la IA en un instante: hacia dónde ir y si usa su objeto.
public readonly record struct DecisionControl(Direccion Direccion, bool UsarObjeto);

public interface IControlador
{
    // Devuelve una intención; el sistema de movimiento valida el recorrido.
    DecisionControl ObtenerDecision(
        Entidad actor, EstadoPartida partida, TimeSpan tiempoSimulacion);
}
