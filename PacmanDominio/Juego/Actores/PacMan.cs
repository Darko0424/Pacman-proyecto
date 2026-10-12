using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Actores;

public sealed class PacMan : Entidad
{
    public int VidasRestantes { get; private set; } =
        ConfiguracionPartida.VidasInicialesPacMan;

    public bool ReaparicionPendiente { get; private set; }

    // Vencimiento relativo al tiempo de simulación, no al reloj del sistema.
    public TimeSpan InvencibleHasta { get; private set; }

    public bool EsEspectador
    {
        get
        {
            return VidasRestantes == 0;
        }
    }

    public bool PuedeParticipar
    {
        get
        {
            return !EsEspectador && !ReaparicionPendiente;
        }
    }

    public PacMan(byte idPersonaje, int idUsuario, Casilla posicionInicial)
        : base(idPersonaje, idUsuario, posicionInicial)
    {
    }

    public bool EsInvencible(TimeSpan tiempoSimulacion)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tiempoSimulacion, TimeSpan.Zero);

        return PuedeParticipar && tiempoSimulacion < InvencibleHasta;
    }

    internal bool PerderVida(TimeSpan tiempoSimulacion)
    {
        if (!PuedeParticipar || EsInvencible(tiempoSimulacion))
        {
            return false;
        }

        VidasRestantes--;
        ReaparicionPendiente = VidasRestantes > 0;
        InvencibleHasta = TimeSpan.Zero;

        return true;
    }

    internal bool Reaparecer(Casilla destino, TimeSpan tiempoSimulacion)
    {
        if (!ReaparicionPendiente || EsEspectador)
        {
            return false;
        }

        var vencimiento = tiempoSimulacion +
            ConfiguracionPartida.DuracionInvencibilidadReaparicion;

        CambiarPosicion(destino);
        InvencibleHasta = vencimiento;
        ReaparicionPendiente = false;

        return true;
    }
}
