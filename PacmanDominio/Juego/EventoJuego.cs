namespace PacmanDominio.Juego;

// Lo que pasó durante un tick, para que el cliente reproduzca sonidos y animaciones.
// Los valores numéricos son estables porque viajan así por la red.
public enum TipoEventoJuego
{
    BolaComida = 1,
    BolaPoderComida = 2,
    FantasmaComido = 3,
    PacManAtrapado = 4,
    // Dos fantasmas chocaron
    Explosion = 5,
    // Dos Pac-Man chocaron con la colisión activada
    Rebote = 6,
    Reaparicion = 7,
    CajaAparecida = 8,
    CajaRecogida = 9,
    ObjetoUsado = 10,
    EfectoTerminado = 11,
    NivelTerminado = 12
}

// IdPersonaje es quien provoca el evento; IdOtroPersonaje, el afectado si lo hay.
public sealed record EventoJuego(
    TipoEventoJuego Tipo,
    byte? IdPersonaje = null,
    byte? IdOtroPersonaje = null,
    int? Fila = null,
    int? Columna = null,
    byte? IdObjeto = null);
