using PacmanLogicaNegocios.Juego.Salas;

namespace PacmanApi.Servicios;

// Al apagar el servidor, cierra el gestor de salas antes de que se disponga el
// contenedor de dependencias: así todavía puede guardar las series interrumpidas.
public sealed class ParadaGestorSalas : IHostedService
{
    private readonly GestorSalas _gestor;
    private readonly ILogger<ParadaGestorSalas> _logger;

    public ParadaGestorSalas(GestorSalas gestor, ILogger<ParadaGestorSalas> logger)
    {
        _gestor = gestor;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _gestor.DisposeAsync();
        }
        catch (AggregateException excepcion)
        {
            // El gestor ya registró cada resultado que no pudo confirmar
            _logger.LogError(excepcion, "Al cerrar quedaron series sin guardar.");
        }
    }
}
