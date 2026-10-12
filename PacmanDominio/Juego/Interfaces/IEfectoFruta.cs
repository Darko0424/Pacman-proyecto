using PacmanDominio.Juego.Items;

namespace PacmanDominio.Juego.Interfaces;

public interface IEfectoFruta
{
    // Clave del catálogo: VELOCIDAD, CONGELAR_FANTASMAS, INVISIBILIDAD o DOBLE_PUNTAJE.
    string TipoEfecto { get; }

    // LN recibe el consumo confirmado y administra su efecto y duración.
    // No modifica la protección defensiva concedida por la reaparición.
    void Aplicar(
        EstadoPartida partida,
        byte idPacMan,
        FrutaEnJuego frutaConsumida,
        TimeSpan tiempoSimulacion);

    // La instancia de fruta permite distinguir aplicaciones, sin fijar cómo se acumulan.
    void Retirar(
        EstadoPartida partida,
        byte idPacMan,
        int idFrutaEnJuego,
        TimeSpan tiempoSimulacion);
}
