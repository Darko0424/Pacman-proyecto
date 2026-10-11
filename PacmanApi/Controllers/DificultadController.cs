using Microsoft.AspNetCore.Mvc;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DificultadController : ControllerBase
{
    private readonly IDificultadLN _dificultadLN;

    public DificultadController(IDificultadLN dificultadLN)
    {
        _dificultadLN = dificultadLN;
    }

    [HttpGet("Listar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _dificultadLN.ListarAsync();

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpGet("Obtener/{id}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(byte id)
    {
        var resultado = await _dificultadLN.ObtenerAsync(
            new TDificultad { IdDificultad = id });

        if (!resultado.Success)
            return NotFound(resultado);

        return Ok(resultado);
    }

    [HttpGet("Buscar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Buscar(string texto = "")
    {
        var resultado = await _dificultadLN.BuscarAsync(
            new TDificultad
            {
                Nombre = texto
            });

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }
}
