using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PacmanApi.Seguridad;
using PacmanDominio.DTO.TiempoReal;
using PacmanDominio.InterfazLN;
using PacmanDominio.Juego.Interfaces;
using PacmanUtilitarios;

namespace PacmanApi.Hubs;

// Adaptador de red: recibe las acciones de los celulares y se las entrega al
// gestor de salas con la identidad ya validada. Nunca recibe posiciones ni estados.
// Las respuestas usan el mismo Respuesta<T> que la API REST.
[Authorize(AuthenticationSchemes = AutenticacionSesion.Esquema)]
public sealed class JuegoHub : Hub
{
    public const string Ruta = "/hubs/juego";

    // Lo que el servidor envía a los clientes
    public const string SalaActualizada = "SalaActualizada";
    public const string NivelIniciado = "NivelIniciado";
    public const string EstadoActualizado = "EstadoActualizado";
    public const string SesionReemplazada = "SesionReemplazada";
    public const string Expulsado = "Expulsado";

    private readonly IGestorSalas _gestor;

    public JuegoHub(IGestorSalas gestor)
    {
        _gestor = gestor;
    }

    // Todos los de una sala reciben sus avisos por este grupo de SignalR
    public static string Grupo(string codigoSala)
    {
        return $"sala-{codigoSala.Trim().ToUpperInvariant()}";
    }

    private int IdUsuario
    {
        get
        {
            return int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }

    private string NombreUsuario
    {
        get
        {
            return Context.User!.FindFirstValue(ClaimTypes.Name)!;
        }
    }

    public Task<Respuesta<IEnumerable<SalaDto>>> ListarSalas()
    {
        return _gestor.ListarAsync();
    }

    public Task<Respuesta<SalaDto>> ObtenerSala(string codigoSala)
    {
        return _gestor.ObtenerAsync(codigoSala);
    }

    public Task<Respuesta<NivelDto>> ObtenerNivel(string codigoSala)
    {
        return _gestor.ObtenerNivelAsync(codigoSala);
    }

    public Task<Respuesta<EstadoPartidaDto>> ObtenerEstado(string codigoSala)
    {
        return _gestor.ObtenerEstadoAsync(codigoSala);
    }

    // Quien crea la sala es su anfitrión
    public async Task<Respuesta<SalaDto>> CrearSala()
    {
        var resultado = await _gestor.CrearAsync(IdUsuario, NombreUsuario, Context.ConnectionId);

        if (resultado.Success && resultado.Data != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(resultado.Data.Codigo));
        }

        return resultado;
    }

    public Task<Respuesta<SalaDto>> UnirseSala(string codigoSala)
    {
        return EntrarAlGrupoAsync(codigoSala, () =>
            _gestor.IngresarAsync(codigoSala, IdUsuario, NombreUsuario, Context.ConnectionId));
    }

    // Con una conexión nueva (por ejemplo, tras perder la señal): conserva la plaza
    // y recibe al instante la sala, el mapa y el estado
    public Task<Respuesta<SalaDto>> ReconectarSala(string codigoSala)
    {
        return EntrarAlGrupoAsync(codigoSala, () =>
            _gestor.ReconectarAsync(codigoSala, IdUsuario, Context.ConnectionId));
    }

    // Igual que ReconectarSala, para un cliente que ya no recuerda el código
    public async Task<Respuesta<SalaDto>> ReanudarSala()
    {
        var sala = await _gestor.ObtenerSalaDeUsuarioAsync(IdUsuario);

        if (!sala.Success || sala.Data == null)
        {
            return sala;
        }

        return await ReconectarSala(sala.Data.Codigo);
    }

    public async Task<Respuesta<SalaDto>> SalirSala(string codigoSala)
    {
        var resultado = await _gestor.SalirAsync(codigoSala, IdUsuario);

        if (resultado.Success)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(codigoSala));
        }

        return resultado;
    }

    // Solo el anfitrión, en el lobby
    public Task<Respuesta<SalaDto>> ConfigurarSala(string codigoSala, ConfiguracionSalaDto configuracion)
    {
        return _gestor.ConfigurarAsync(codigoSala, IdUsuario, configuracion);
    }

    // PACMAN, FANTASMA o null (sorteo); solo cuenta si juega solo contra la IA
    public Task<Respuesta<SalaDto>> ElegirRol(string codigoSala, string? rol)
    {
        return _gestor.ElegirRolAsync(codigoSala, IdUsuario, rol);
    }

    // Solo el anfitrión, en el lobby
    public Task<Respuesta<SalaDto>> BloquearSala(string codigoSala, bool bloqueada)
    {
        return _gestor.BloquearAsync(codigoSala, IdUsuario, bloqueada);
    }

    // Solo el anfitrión, en el lobby
    public Task<Respuesta<SalaDto>> ExpulsarJugador(string codigoSala, int idJugador)
    {
        return _gestor.ExpulsarAsync(codigoSala, IdUsuario, idJugador);
    }

    // Solo el anfitrión
    public Task<Respuesta<SalaDto>> IniciarPartida(string codigoSala)
    {
        return _gestor.IniciarPartidaAsync(codigoSala, IdUsuario);
    }

    // direccion: 1 arriba, 2 abajo, 3 izquierda, 4 derecha
    public Task<Respuesta<bool>> EnviarDireccion(string codigoSala, Direccion direccion)
    {
        if (!Enum.IsDefined(direccion))
        {
            return Task.FromResult(Respuesta<bool>.Fail("Dirección no válida."));
        }

        return _gestor.EnviarDireccionAsync(
            codigoSala, IdUsuario, Context.ConnectionId, new DireccionDto(direccion));
    }

    // Botón de acción
    public Task<Respuesta<bool>> UsarObjeto(string codigoSala)
    {
        return _gestor.UsarObjetoAsync(codigoSala, IdUsuario, Context.ConnectionId);
    }

    // La plaza se conserva: la IA mueve su personaje hasta que se reconecte
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _gestor.DesconectarAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // Se une al grupo antes de la operación para recibir también el aviso que
    // provoca; si la operación falla, sale del grupo.
    private async Task<Respuesta<SalaDto>> EntrarAlGrupoAsync(
        string codigoSala, Func<Task<Respuesta<SalaDto>>> operacion)
    {
        if (string.IsNullOrWhiteSpace(codigoSala))
        {
            return Respuesta<SalaDto>.Fail("Falta el código de sala.");
        }

        var grupo = Grupo(codigoSala);
        await Groups.AddToGroupAsync(Context.ConnectionId, grupo);

        var resultado = await operacion();

        if (!resultado.Success)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, grupo);
        }

        return resultado;
    }
}
