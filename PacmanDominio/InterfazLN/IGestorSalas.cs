using PacmanDominio.DTO.TiempoReal;
using PacmanDominio.Juego;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface IGestorSalas
{
    // El gestor serializa las acciones y utiliza las operaciones validadas de Sala.
    // Los códigos y las identidades llegan resueltos por la capa que lo invoque.
    Task<Respuesta<SalaDto>> CrearAsync(string codigoSala);
    Task<Respuesta<IEnumerable<SalaDto>>> ListarAsync();
    Task<Respuesta<SalaDto>> ObtenerAsync(string codigoSala);

    Task<Respuesta<SalaDto>> IngresarAsync(string codigoSala, int idUsuario, string idConexion);
    Task<Respuesta<SalaDto>> AsignarPersonajeAsync(string codigoSala, int idUsuario, byte? idPersonaje);
    Task<Respuesta<SalaDto>> SalirAsync(string codigoSala, int idUsuario);
    Task<Respuesta<SalaDto>> DesconectarAsync(string codigoSala, string idConexion);
    Task<Respuesta<SalaDto>> ReconectarAsync(string codigoSala, int idUsuario, string idConexion);

    // El estado se construye en el servidor y se acepta mediante Sala.IniciarPartida.
    // Nunca se reconstruye un estado vivo desde el DTO recibido de un cliente.
    Task<Respuesta<EstadoPartidaDto>> IniciarPartidaAsync(string codigoSala, EstadoPartida partida);
    Task<Respuesta<EstadoPartidaDto>> ObtenerEstadoAsync(string codigoSala);

    // Verifica que la conexión vigente pertenezca al usuario y a su actor humano.
    // La dirección se entrega al controlador; no permite asignar posiciones desde la red.
    Task<Respuesta<bool>> EnviarDireccionAsync(
        string codigoSala, int idUsuario, string idConexion, DireccionDto direccion);
}
