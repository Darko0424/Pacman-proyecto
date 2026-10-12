using PacmanDominio.EntidadesTipadas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface IPartidaLN
{
    // Valida y guarda conjuntamente el resultado de la partida y sus participantes.
    // La implementación debe evitar resultados parciales y duplicados al reintentar.
    Task<Respuesta<TPartida>> GuardarResultadoAsync(
        TPartida partida, IReadOnlyCollection<TParticipantePartida> participantes);

    Task<Respuesta<TPartida>> ObtenerAsync(TPartida datos);
    Task<Respuesta<IEnumerable<TParticipantePartida>>> ListarParticipantesAsync(int idPartida);
}
