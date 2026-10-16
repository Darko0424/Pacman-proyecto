namespace PacmanDominio.Juego.Items;

// Objeto usado cuyo efecto sigue vigente. Qué hace cada efecto lo decide la
// implementación de IEfectoObjeto asociada a TipoEfecto.
public sealed class EfectoActivo
{
    public int IdEfecto { get; }
    public byte IdObjeto { get; }

    // Clave de objetos.efecto
    public string TipoEfecto { get; }

    public byte IdPersonajeOrigen { get; }
    public Bando BandoOrigen { get; }
    public TimeSpan Inicio { get; }
    public TimeSpan Fin { get; }

    public EfectoActivo(
        int idEfecto,
        byte idObjeto,
        string tipoEfecto,
        byte idPersonajeOrigen,
        Bando bandoOrigen,
        TimeSpan inicio,
        TimeSpan fin)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idEfecto);
        ArgumentOutOfRangeException.ThrowIfZero(idObjeto);
        ArgumentException.ThrowIfNullOrWhiteSpace(tipoEfecto);
        ArgumentOutOfRangeException.ThrowIfZero(idPersonajeOrigen);
        ArgumentOutOfRangeException.ThrowIfLessThan(inicio, TimeSpan.Zero);

        if (!Enum.IsDefined(bandoOrigen))
        {
            throw new ArgumentOutOfRangeException(nameof(bandoOrigen));
        }

        if (fin <= inicio)
        {
            throw new ArgumentException("El efecto debe terminar después de empezar.", nameof(fin));
        }

        IdEfecto = idEfecto;
        IdObjeto = idObjeto;
        TipoEfecto = tipoEfecto;
        IdPersonajeOrigen = idPersonajeOrigen;
        BandoOrigen = bandoOrigen;
        Inicio = inicio;
        Fin = fin;
    }

    public bool EstaVigente(TimeSpan tiempoSimulacion)
    {
        return tiempoSimulacion >= Inicio && tiempoSimulacion < Fin;
    }
}
