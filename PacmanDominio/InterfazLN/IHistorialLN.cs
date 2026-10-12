using PacmanDominio.Entidades.Vistas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface IHistorialLN
{
    // Consultas de solo lectura sobre las vistas existentes.
    Task<Respuesta<IEnumerable<HistorialEntrada>>> ListarPorUsuarioAsync(int idUsuario);
    Task<Respuesta<ProgresoJugador>> ObtenerProgresoAsync(int idUsuario);
}
