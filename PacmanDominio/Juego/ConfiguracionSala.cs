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

    // Segundos que tarda un fantasma en volver en el modo clásico
    public static IReadOnlyList<int> OpcionesReaparicionFantasmas { get; } = [30, 20, 10, 5];
    public const int ReaparicionFantasmasPorDefecto = 10;

    public static ConfiguracionSala PorDefecto { get; } = new(
        IdDificultadPorDefecto, VidasPorDefecto, true, FrecuenciaCajas.Normal,
        false, false, ReaparicionFantasmasPorDefecto);

    // La existencia en el catálogo la valida la lógica de negocios
    public byte IdDificultad { get; }

    // Las tienen tanto los Pac-Man como los fantasmas, y se reinician en cada nivel
    public int Vidas { get; }

    // true: los Pac-Man rebotan al chocar entre sí
    public bool ColisionEntrePacMan { get; }

    public FrecuenciaCajas FrecuenciaCajas { get; }

    // true: los roles se sortean solo en el primer nivel y cada jugador conserva
    // su personaje. Contra la IA el rol ya es fijo, así que solo cuenta en multijugador.
    public bool BandosFijos { get; }

    // Como el arcade: los fantasmas no se quedan sin vidas y los Pac-Man solo
    // ganan comiéndose todas las bolas. Las vidas configuradas son de los Pac-Man.
    public bool ModoClasico { get; }

    // Solo se usa en el modo clásico; en el normal todos tardan ReglasJuego.DemoraReaparicion
    public int SegundosReaparicionFantasmas { get; }

    public ConfiguracionSala(
        byte idDificultad,
        int vidas,
        bool colisionEntrePacMan,
        FrecuenciaCajas frecuenciaCajas,
        bool bandosFijos,
        bool modoClasico,
        int segundosReaparicionFantasmas)
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

        if (!OpcionesReaparicionFantasmas.Contains(segundosReaparicionFantasmas))
        {
            throw new ArgumentOutOfRangeException(
                nameof(segundosReaparicionFantasmas),
                $"La reaparición de los fantasmas debe ser de {string.Join(", ", OpcionesReaparicionFantasmas)} segundos.");
        }

        IdDificultad = idDificultad;
        Vidas = vidas;
        ColisionEntrePacMan = colisionEntrePacMan;
        FrecuenciaCajas = frecuenciaCajas;
        BandosFijos = bandosFijos;
        ModoClasico = modoClasico;
        SegundosReaparicionFantasmas = segundosReaparicionFantasmas;
    }
}
