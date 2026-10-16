using PacmanDominio.EntidadesTipadas;
using PacmanDominio.Juego;
using PacmanDominio.Juego.Salas;

namespace PacmanLogicaNegocios.Juego.Salas;

// Solo el gestor accede a estos datos, siempre dentro de Compuerta.
internal sealed class ContextoSala
{
    public Sala Sala { get; set; }
    public SemaphoreSlim Compuerta { get; } = new(1, 1);
    public PeriodicTimer Temporizador { get; }
    public Task Ciclo { get; set; } = Task.CompletedTask;
    public bool Retirada { get; set; }
    public InicioSala? InicioPendiente { get; set; }
    public NivelEnSala? Nivel { get; set; }
    public long UltimoAvance { get; set; }
    public long? TransicionDesde { get; set; }
    public long? UltimoFallo { get; set; }
    public bool Abandonada { get; set; }
    public TPartida? CierrePendiente { get; set; }
    public bool CierreGuardado { get; set; }
    public bool SalaPendiente { get; set; } = true;
    public bool NivelPendiente { get; set; }
    public bool EstadoPendiente { get; set; }
    public IReadOnlyList<EventoJuego> Eventos { get; set; } = Array.Empty<EventoJuego>();

    public ContextoSala(Sala sala, TimeProvider tiempo)
    {
        Sala = sala;
        Temporizador = new PeriodicTimer(ReglasJuego.DuracionTick, tiempo);
    }
}

internal sealed class NivelEnSala
{
    public MotorPartida Motor { get; }
    public int IdNivel { get; }
    public DateTime FechaInicio { get; }
    public TRonda? RondaPendiente { get; set; }
    public IReadOnlyList<TParticipanteRonda>? ParticipantesPendientes { get; set; }
    public bool RondaGuardada { get; set; }

    public NivelEnSala(MotorPartida motor, int idNivel, DateTime fechaInicio)
    {
        Motor = motor;
        IdNivel = idNivel;
        FechaInicio = fechaInicio;
    }
}

// Se valida el inicio sobre una copia de la sala; se publica al confirmar el ID.
// Así un fallo de BD conserva el lobby y la misma clave idempotente de reintento.
internal sealed class InicioSala
{
    public Sala SalaPreparada { get; }
    public NivelEnSala Nivel { get; }
    public TPartida Datos { get; }

    public InicioSala(Sala salaPreparada, NivelEnSala nivel, TPartida datos)
    {
        SalaPreparada = salaPreparada;
        Nivel = nivel;
        Datos = datos;
    }
}
