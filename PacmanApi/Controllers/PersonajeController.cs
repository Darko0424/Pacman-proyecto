using Microsoft.AspNetCore.Mvc;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PersonajeController : ControllerBase
{
    private readonly IPersonajeLN _personajeLN;

    public PersonajeController(IPersonajeLN personajeLN)
    {
        _personajeLN = personajeLN;
    }

    [HttpGet("Listar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _personajeLN.ListarAsync();

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpGet("Obtener/{id}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(byte id)
    {
        var resultado = await _personajeLN.ObtenerAsync(
            new TPersonaje { IdPersonaje = id });

        if (!resultado.Success)
            return NotFound(resultado);

        return Ok(resultado);
    }

    [HttpGet("Buscar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Buscar(string texto = "")
    {
        var resultado = await _personajeLN.BuscarAsync(
            new TPersonaje
            {
                Nombre = texto
            });

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }
}
