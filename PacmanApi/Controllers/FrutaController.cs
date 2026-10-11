using Microsoft.AspNetCore.Mvc;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FrutaController : ControllerBase
{
    private readonly IFrutaLN _frutaLN;

    public FrutaController(IFrutaLN frutaLN)
    {
        _frutaLN = frutaLN;
    }

    [HttpGet("Listar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _frutaLN.ListarAsync();

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpGet("Obtener/{id}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(byte id)
    {
        var resultado = await _frutaLN.ObtenerAsync(
            new TFruta { IdFruta = id });

        if (!resultado.Success)
            return NotFound(resultado);

        return Ok(resultado);
    }

    [HttpGet("Buscar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Buscar(string texto = "")
    {
        var resultado = await _frutaLN.BuscarAsync(
            new TFruta
            {
                Nombre = texto
            });

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }
}
