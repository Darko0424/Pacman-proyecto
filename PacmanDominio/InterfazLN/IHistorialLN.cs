using PacmanDominio.Entidades.Vistas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface IHistorialLN
{
    // Consultas de solo lectura sobre las vistas existentes.
    Task<Respuesta<IEnumerable<HistorialEntrada>>> ListarPorUsuarioAsync(int idUsuario);

    // Detalle por nivel de una partida del usuario
    Task<Respuesta<IEnumerable<HistorialRonda>>> ListarRondasAsync(int idUsuario, int idPartida);

    Task<Respuesta<ProgresoJugador>> ObtenerProgresoAsync(int idUsuario);
}
