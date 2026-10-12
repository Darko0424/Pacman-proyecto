using PacmanDominio.DTO.TiempoReal;

namespace PacmanDominio.InterfazLN;

public interface INotificadorJuego
{
    // El destino se pasa separado de los datos públicos, sin acoplarse a SignalR.
    Task NotificarSalaAsync(string codigoSala, SalaDto sala);
    Task NotificarEstadoAsync(string codigoSala, EstadoPartidaDto estado);
    Task EnviarSalaAConexionAsync(string idConexion, SalaDto sala);
    Task EnviarEstadoAConexionAsync(string idConexion, EstadoPartidaDto estado);
}
