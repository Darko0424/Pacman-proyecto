using Microsoft.AspNetCore.SignalR;
using PacmanDominio.DTO.TiempoReal;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Hubs;

// Implementación de INotificadorJuego con SignalR. IHubContext es seguro para
// varios hilos, así que un solo notificador atiende a todas las salas.
public sealed class NotificadorJuego : INotificadorJuego
{
    private readonly IHubContext<JuegoHub> _hub;

    public NotificadorJuego(IHubContext<JuegoHub> hub)
    {
        _hub = hub;
    }

    public Task NotificarSalaAsync(string codigoSala, SalaDto sala)
    {
        return _hub.Clients.Group(JuegoHub.Grupo(codigoSala))
            .SendAsync(JuegoHub.SalaActualizada, sala);
    }

    public Task NotificarNivelAsync(string codigoSala, NivelDto nivel)
    {
        return _hub.Clients.Group(JuegoHub.Grupo(codigoSala))
            .SendAsync(JuegoHub.NivelIniciado, nivel);
    }

    public Task NotificarEstadoAsync(string codigoSala, EstadoPartidaDto estado)
    {
        return _hub.Clients.Group(JuegoHub.Grupo(codigoSala))
            .SendAsync(JuegoHub.EstadoActualizado, estado);
    }

    public Task EnviarSalaAConexionAsync(string idConexion, SalaDto sala)
    {
        return _hub.Clients.Client(idConexion).SendAsync(JuegoHub.SalaActualizada, sala);
    }

    public Task EnviarNivelAConexionAsync(string idConexion, NivelDto nivel)
    {
        return _hub.Clients.Client(idConexion).SendAsync(JuegoHub.NivelIniciado, nivel);
    }

    public Task EnviarEstadoAConexionAsync(string idConexion, EstadoPartidaDto estado)
    {
        return _hub.Clients.Client(idConexion).SendAsync(JuegoHub.EstadoActualizado, estado);
    }
}
