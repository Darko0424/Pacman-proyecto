using PacmanDominio.DTO.TiempoReal;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface IGestorSalas
{
    // El gestor serializa las acciones de cada sala y utiliza las operaciones validadas de Sala.
    // Las identidades llegan resueltas por la capa que lo invoque (sesión ya validada).

    // Genera el código; quien crea la sala entra como anfitrión. Máximo 5 salas.
    Task<Respuesta<SalaDto>> CrearAsync(int idUsuario, string nombreUsuario, string idConexion);
    Task<Respuesta<IEnumerable<SalaDto>>> ListarAsync();
    Task<Respuesta<SalaDto>> ObtenerAsync(string codigoSala);

    Task<Respuesta<SalaDto>> IngresarAsync(
        string codigoSala, int idUsuario, string nombreUsuario, string idConexion);
    Task<Respuesta<SalaDto>> SalirAsync(string codigoSala, int idUsuario);

    // El gestor encuentra la sala a partir de la conexión que se cerró
    Task<Respuesta<SalaDto>> DesconectarAsync(string idConexion);
    Task<Respuesta<SalaDto>> ReconectarAsync(string codigoSala, int idUsuario, string idConexion);

    // Solo el anfitrión, en el lobby
    Task<Respuesta<SalaDto>> ConfigurarAsync(
        string codigoSala, int idUsuario, ConfiguracionSalaDto configuracion);

    // Preferencia para jugar solo contra la IA: PACMAN, FANTASMA o null (sorteo)
    Task<Respuesta<SalaDto>> ElegirRolAsync(string codigoSala, int idUsuario, string? rol);

    // Solo el anfitrión. El estado se construye en el servidor (nivel 1, roles sorteados
    // y relleno con IA); nunca se reconstruye un estado vivo desde datos del cliente.
    Task<Respuesta<SalaDto>> IniciarPartidaAsync(string codigoSala, int idUsuario);

    Task<Respuesta<NivelDto>> ObtenerNivelAsync(string codigoSala);
    Task<Respuesta<EstadoPartidaDto>> ObtenerEstadoAsync(string codigoSala);

    // Verifica que la conexión vigente pertenezca al usuario y a su personaje.
    // La dirección se entrega al controlador; no permite asignar posiciones desde la red.
    Task<Respuesta<bool>> EnviarDireccionAsync(
        string codigoSala, int idUsuario, string idConexion, DireccionDto direccion);

    // Botón de acción: usa el objeto guardado del personaje del usuario
    Task<Respuesta<bool>> UsarObjetoAsync(string codigoSala, int idUsuario, string idConexion);
}
