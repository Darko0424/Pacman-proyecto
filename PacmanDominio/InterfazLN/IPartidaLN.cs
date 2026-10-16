using PacmanDominio.EntidadesTipadas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

// La partida se guarda por partes mientras se juega: al iniciar la serie,
// al terminar cada nivel (ronda) y al terminar la serie. Todas las operaciones
// toleran reintentos sin duplicar datos.
public interface IPartidaLN
{
    // Registra la serie EN_CURSO. Un reintento (misma sala y fecha de inicio)
    // devuelve la partida ya registrada.
    Task<Respuesta<TPartida>> IniciarAsync(TPartida partida);

    // Guarda conjuntamente un nivel y sus participantes. Un reintento del mismo
    // nivel devuelve la ronda ya guardada.
    Task<Respuesta<TRonda>> GuardarRondaAsync(
        TRonda ronda, IReadOnlyCollection<TParticipanteRonda> participantes);

    // Cierra la serie como FINALIZADA o ABANDONADA
    Task<Respuesta<TPartida>> FinalizarAsync(TPartida datos);

    Task<Respuesta<TPartida>> ObtenerAsync(TPartida datos);
    Task<Respuesta<IEnumerable<TRonda>>> ListarRondasAsync(int idPartida);
    Task<Respuesta<IEnumerable<TParticipanteRonda>>> ListarParticipantesAsync(int idRonda);
}
