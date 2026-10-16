namespace PacmanDominio.Juego;

// Reglas fijas del juego. Lo que el anfitrión puede cambiar está en ConfiguracionSala.
public static class ConfiguracionPartida
{
    public const int CantidadNiveles = 5;

    // Multijugador: siempre 2 Pac-Man y 3 fantasmas. Los humanos ocupan como
    // máximo 2 Pac-Man y 2 fantasmas; el tercer fantasma siempre es IA.
    public const int CantidadPacMan = 2;
    public const int CantidadFantasmas = 3;
    public const int CantidadMaximaHumanos = 4;
    public const int CantidadMaximaPacManHumanos = 2;
    public const int CantidadMaximaFantasmasHumanos = 2;

    // Contra la IA: 1 Pac-Man y los 3 fantasmas, con un único humano.
    public const int CantidadPacManContraIa = 1;

    public const int CantidadPuntosReaparicion = 6;

    public static TimeSpan DuracionInvencibilidadReaparicion { get; } =
        TimeSpan.FromSeconds(3);
}
