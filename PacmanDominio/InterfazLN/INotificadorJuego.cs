using PacmanDominio.DTO.TiempoReal;

namespace PacmanDominio.InterfazLN;

public interface INotificadorJuego
{
    // El destino se pasa separado de los datos públicos, sin acoplarse a SignalR.
    Task NotificarSalaAsync(string codigoSala, SalaDto sala);

    // Al empezar cada nivel (el mapa no cambia durante el nivel)
    Task NotificarNivelAsync(string codigoSala, NivelDto nivel);

    Task NotificarEstadoAsync(string codigoSala, EstadoPartidaDto estado);
    Task EnviarSalaAConexionAsync(string idConexion, SalaDto sala);
    Task EnviarNivelAConexionAsync(string idConexion, NivelDto nivel);
    Task EnviarEstadoAConexionAsync(string idConexion, EstadoPartidaDto estado);

    //Notifica cuando otra conexion del mismo usuario toma su lugar (se inicio sesión en otro dispositivo)
    Task NotificarSesionReemplazadaAsync(string idConexion, string codigoSala);

    Task NotificarSesionCerradaAsync(int idUsuario);
}
