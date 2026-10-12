namespace PacmanDominio.Juego;

public static class ConfiguracionPartida
{
    public const int CantidadPacMan = 2;
    public const int CantidadFantasmasHumanos = 2;
    public const int CantidadFantasmasIa = 1;
    public const int VidasInicialesPacMan = 3;
    public const int CantidadPuntosReaparicion = 6;

    public static TimeSpan DuracionInvencibilidadReaparicion { get; } =
        TimeSpan.FromSeconds(3);
}
