using Microsoft.AspNetCore.Mvc;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NivelController : ControllerBase
{
    private readonly INivelLN _nivelLN;

    public NivelController(INivelLN nivelLN)
    {
        _nivelLN = nivelLN;
    }

    [HttpGet("Listar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _nivelLN.ListarAsync();

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpGet("Obtener/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(int id)
    {
        var resultado = await _nivelLN.ObtenerAsync(
            new TNivel { IdNivel = id });

        if (!resultado.Success)
            return NotFound(resultado);

        return Ok(resultado);
    }

    [HttpGet("Buscar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Buscar(string texto = "")
    {
        var resultado = await _nivelLN.BuscarAsync(
            new TNivel
            {
                Nombre = texto
            });

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }
}
