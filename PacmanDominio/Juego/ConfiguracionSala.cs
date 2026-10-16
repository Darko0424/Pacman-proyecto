namespace PacmanDominio.Juego;

// Cada cuánto aparecen cajas de objetos; el motor traduce el valor a segundos.
public enum FrecuenciaCajas
{
    Ninguna = 0,
    Baja = 1,
    Normal = 2,
    Alta = 3
}

// Opciones del menú de la sala, que solo cambia el anfitrión.
// Es inmutable: cada cambio crea una configuración nueva, así la partida en
// curso conserva la que tenía al empezar.
public sealed record ConfiguracionSala
{
    public const int VidasMinimas = 1;
    public const int VidasMaximas = 5;
    public const int VidasPorDefecto = 3;

    // NORMAL en el catálogo de dificultades
    public const byte IdDificultadPorDefecto = 2;

    public static ConfiguracionSala PorDefecto { get; } = new(
        IdDificultadPorDefecto, VidasPorDefecto, true, FrecuenciaCajas.Normal);

    // La existencia en el catálogo la valida la lógica de negocios
    public byte IdDificultad { get; }

    // Las tienen tanto los Pac-Man como los fantasmas, y se reinician en cada nivel
    public int Vidas { get; }

    // true: los Pac-Man rebotan al chocar entre sí
    public bool ColisionEntrePacMan { get; }

    public FrecuenciaCajas FrecuenciaCajas { get; }

    public ConfiguracionSala(
        byte idDificultad,
        int vidas,
        bool colisionEntrePacMan,
        FrecuenciaCajas frecuenciaCajas)
    {
        ArgumentOutOfRangeException.ThrowIfZero(idDificultad);

        if (vidas < VidasMinimas || vidas > VidasMaximas)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vidas),
                $"Las vidas deben estar entre {VidasMinimas} y {VidasMaximas}.");
        }

        if (!Enum.IsDefined(frecuenciaCajas))
        {
            throw new ArgumentOutOfRangeException(
                nameof(frecuenciaCajas), "Frecuencia de cajas no válida.");
        }

        IdDificultad = idDificultad;
        Vidas = vidas;
        ColisionEntrePacMan = colisionEntrePacMan;
        FrecuenciaCajas = frecuenciaCajas;
    }
}
