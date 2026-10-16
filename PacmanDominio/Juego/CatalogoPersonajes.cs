namespace PacmanDominio.Juego;

public enum Bando
{
    PacMan = 1,
    Fantasma = 2
}

// Identificadores y códigos definidos en el catálogo SQL (personajes.rol).
public static class CatalogoPersonajes
{
    public const string CodigoPacMan = "PACMAN";
    public const string CodigoFantasma = "FANTASMA";

    public static IReadOnlyList<byte> IdsPacMan { get; } = new byte[] { 1, 2 };
    public static IReadOnlyList<byte> IdsFantasmas { get; } = new byte[] { 3, 4, 5 };

    public static bool EsPacMan(byte? idPersonaje)
    {
        return idPersonaje is 1 or 2;
    }

    public static bool EsFantasma(byte? idPersonaje)
    {
        return idPersonaje is 3 or 4 or 5;
    }

    public static Bando? ObtenerBando(byte? idPersonaje)
    {
        if (EsPacMan(idPersonaje))
        {
            return Bando.PacMan;
        }

        return EsFantasma(idPersonaje) ? Bando.Fantasma : null;
    }

    public static string ACodigo(Bando bando)
    {
        return bando switch
        {
            Bando.PacMan => CodigoPacMan,
            Bando.Fantasma => CodigoFantasma,
            _ => throw new ArgumentOutOfRangeException(nameof(bando))
        };
    }

    public static Bando? DesdeCodigo(string? codigo)
    {
        return (codigo ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            CodigoPacMan => Bando.PacMan,
            CodigoFantasma => Bando.Fantasma,
            _ => null
        };
    }
}
