namespace PacmanDominio.Juego;

// Con un solo jugador en la sala la partida es contra la IA.
public enum ModoPartida
{
    Multijugador = 1,
    ContraIa = 2
}

// Códigos definidos en partidas.modo.
public static class ModosPartida
{
    public const string CodigoMultijugador = "MULTIJUGADOR";
    public const string CodigoContraIa = "CONTRA_IA";

    public static string ACodigo(ModoPartida modo)
    {
        return modo switch
        {
            ModoPartida.Multijugador => CodigoMultijugador,
            ModoPartida.ContraIa => CodigoContraIa,
            _ => throw new ArgumentOutOfRangeException(nameof(modo))
        };
    }

    public static ModoPartida? DesdeCodigo(string? codigo)
    {
        return (codigo ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            CodigoMultijugador => ModoPartida.Multijugador,
            CodigoContraIa => ModoPartida.ContraIa,
            _ => null
        };
    }
}
