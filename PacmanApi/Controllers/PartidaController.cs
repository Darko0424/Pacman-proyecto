using Microsoft.AspNetCore.Mvc;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Controllers;

// Solo lectura: el resultado lo guarda el motor en el servidor, nunca un cliente
[Route("api/[controller]")]
[ApiController]
public class PartidaController : ControllerBase
{
    private readonly IPartidaLN _partidaLN;

    public PartidaController(IPartidaLN partidaLN)
    {
        _partidaLN = partidaLN;
    }

    [HttpGet("Obtener/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(int id)
    {
        var resultado = await _partidaLN.ObtenerAsync(
            new TPartida { IdPartida = id });

        if (!resultado.Success)
            return NotFound(resultado);

        return Ok(resultado);
    }

    [HttpGet("Participantes/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Participantes(int id)
    {
        var resultado = await _partidaLN.ListarParticipantesAsync(id);

        if (!resultado.Success)
            return NotFound(resultado);

        return Ok(resultado);
    }
}
