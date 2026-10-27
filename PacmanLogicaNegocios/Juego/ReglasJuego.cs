using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;

namespace PacmanLogicaNegocios.Juego;

// Valores del juego que se pueden ajustar sin tocar la lógica del motor.
public static class ReglasJuego
{
    // Paso fijo de la simulación: 20 actualizaciones por segundo
    public static TimeSpan DuracionTick { get; } = TimeSpan.FromMilliseconds(50);

    // Velocidades en casillas por segundo. Se multiplican por la velocidad del
    // nivel, por la dificultad (solo fantasmas de la IA) y por los efectos.
    public const double VelocidadPacMan = 6.0;
    public const double VelocidadFantasma = 5.6;
    public const double FactorFantasmaAsustado = 0.5;

    // Como en el clásico, Pac-Man va más lento mientras come: así los fantasmas
    // lo alcanzan en pasillos con bolas, pero no en los que ya limpió.
    public const double FactorPacManComiendo = 0.87;

    // Con ticks de 50 ms nadie avanza más de una casilla por tick
    public const double VelocidadMaxima = 19.0;

    public const int PuntosBola = 10;
    public const int PuntosBolaPoder = 50;
    public const int PuntosFantasmaComido = 200;
    public const int PuntosPacManAtrapado = 300;

    // Para cada integrante del bando que gana el nivel
    public const int PuntosNivelGanado = 500;

    // Vuelta al lobby entre niveles (la aplica el gestor de salas)
    public static TimeSpan DuracionTransicion { get; } = TimeSpan.FromSeconds(5);

    // Tiempo entre perder una vida y reaparecer
    public static TimeSpan DemoraReaparicion { get; } = TimeSpan.FromSeconds(2);

    // En el modo clásico los fantasmas tardan lo que elija el anfitrión
    public static TimeSpan DemoraReaparicionDe(Entidad actor, ConfiguracionSala configuracion)
    {
        return actor.Bando == Bando.Fantasma && configuracion.ModoClasico
            ? TimeSpan.FromSeconds(configuracion.SegundosReaparicionFantasmas)
            : DemoraReaparicion;
    }

    // Dos fantasmas que chocan quedan aturdidos y vuelven a un punto de reaparición
    public static TimeSpan DuracionAturdimiento { get; } = TimeSpan.FromSeconds(2);

    // Fantasmas de la IA: alternan dispersión (cada uno a su esquina) y persecución,
    // empezando por dispersión. Terminado el ciclo, persiguen hasta el final del nivel.
    // Más corto que el clásico: con dos Pac-Man el mapa se limpia en un minuto.
    public static IReadOnlyList<TimeSpan> CicloModos { get; } = new[]
    {
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(3)
    };

    // La IA de Pac-Man huye si un fantasma peligroso está a esta distancia o menos
    public const int DistanciaPeligroPacMan = 6;

    // La IA de Pac-Man persigue fantasmas asustados a esta distancia o menos
    public const int DistanciaPersecucionPresa = 12;

    // La IA usa su objeto cuando un rival está a esta distancia o menos
    public const int DistanciaUsoObjeto = 6;

    // Los fantasmas de la IA evitan pisar la casilla de otro fantasma (explosión)
    public const int PenalizacionCasillaOcupada = 4;

    // Objetos
    public const double FactorRalentizar = 0.5;
    public const int MultiplicadorPuntosDobles = 2;

    // El teletransporte avanza en línea recta hasta este número de casillas
    public const int DistanciaTeletransporte = 6;

    // Lo que queda congelado el Pac-Man que pisa una trampa de hielo
    public static TimeSpan DuracionCongeladoTrampa { get; } = TimeSpan.FromSeconds(3);

    // Cada cuánto aparece una caja; null = sin cajas
    public static TimeSpan? IntervaloCajas(FrecuenciaCajas frecuencia)
    {
        return frecuencia switch
        {
            FrecuenciaCajas.Baja => TimeSpan.FromSeconds(20),
            FrecuenciaCajas.Normal => TimeSpan.FromSeconds(12),
            FrecuenciaCajas.Alta => TimeSpan.FromSeconds(6),
            _ => null
        };
    }
}
