using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Actores;

// Estado de un personaje durante un nivel. Las vidas y estadísticas empiezan
// de cero en cada nivel porque los roles se vuelven a sortear.
public abstract class Entidad
{
    public byte IdPersonaje { get; }

    // Dueño del personaje para el historial; null = la IA lo controla desde el inicio
    public int? IdUsuario { get; }

    public Casilla Posicion { get; private set; }
    public int VidasRestantes { get; private set; }
    public bool ReaparicionPendiente { get; private set; }

    // Vencimiento relativo al tiempo de simulación, no al reloj del sistema.
    public TimeSpan InvencibleHasta { get; private set; }

    // Un humano desconectado o que abandonó: la IA mueve su personaje
    public bool ControlIa { get; private set; }

    // El jugador se fue; ya no puede recuperar el control
    public bool Abandono { get; private set; }

    // Espacio para un objeto, que se usa con el botón de acción
    public byte? IdObjetoGuardado { get; private set; }

    public int Puntuacion { get; private set; }
    public int FantasmasComidos { get; private set; }
    public int PacmansAtrapados { get; private set; }
    public int ObjetosUsados { get; private set; }

    public abstract Bando Bando { get; }

    public bool EsIa
    {
        get
        {
            return IdUsuario == null;
        }
    }

    public bool Eliminado
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
            return !Eliminado && !ReaparicionPendiente;
        }
    }

    protected Entidad(byte idPersonaje, int? idUsuario, Casilla posicionInicial, int vidas)
    {
        ArgumentOutOfRangeException.ThrowIfZero(idPersonaje);
        ArgumentNullException.ThrowIfNull(posicionInicial);

        if (idUsuario.HasValue && idUsuario.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(idUsuario));
        }

        if (vidas < ConfiguracionSala.VidasMinimas || vidas > ConfiguracionSala.VidasMaximas)
        {
            throw new ArgumentOutOfRangeException(nameof(vidas));
        }

        if (!posicionInicial.EsTransitable)
        {
            throw new ArgumentException(
                "La posición inicial debe ser transitable.",
                nameof(posicionInicial));
        }

        IdPersonaje = idPersonaje;
        IdUsuario = idUsuario;
        Posicion = posicionInicial;
        VidasRestantes = vidas;
        ControlIa = idUsuario == null;
    }

    public bool EsInvencible(TimeSpan tiempoSimulacion)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tiempoSimulacion, TimeSpan.Zero);

        return PuedeParticipar && tiempoSimulacion < InvencibleHasta;
    }

    internal void CambiarPosicion(Casilla destino)
    {
        Posicion = destino;
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
        if (!ReaparicionPendiente || Eliminado)
        {
            return false;
        }

        CambiarPosicion(destino);
        InvencibleHasta = tiempoSimulacion +
            ConfiguracionPartida.DuracionInvencibilidadReaparicion;
        ReaparicionPendiente = false;

        return true;
    }

    internal void SumarPuntos(int puntos)
    {
        Puntuacion += puntos;
    }

    internal void RegistrarFantasmaComido()
    {
        FantasmasComidos++;
    }

    internal void RegistrarPacManAtrapado()
    {
        PacmansAtrapados++;
    }

    internal bool GuardarObjeto(byte idObjeto)
    {
        if (IdObjetoGuardado.HasValue)
        {
            return false;
        }

        IdObjetoGuardado = idObjeto;

        return true;
    }

    internal byte? UsarObjeto()
    {
        var idObjeto = IdObjetoGuardado;

        if (!idObjeto.HasValue)
        {
            return null;
        }

        IdObjetoGuardado = null;
        ObjetosUsados++;

        return idObjeto;
    }

    internal bool CederControlAIa(bool abandono)
    {
        if (EsIa)
        {
            return false;
        }

        ControlIa = true;
        Abandono = Abandono || abandono;

        return true;
    }

    internal bool RecuperarControl()
    {
        if (EsIa || Abandono || !ControlIa)
        {
            return false;
        }

        ControlIa = false;

        return true;
    }
}
