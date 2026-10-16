using Microsoft.AspNetCore.Mvc;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ObjetoController : ControllerBase
{
    private readonly IObjetoLN _objetoLN;

    public ObjetoController(IObjetoLN objetoLN)
    {
        _objetoLN = objetoLN;
    }

    [HttpGet("Listar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _objetoLN.ListarAsync();

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpGet("Obtener/{id}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(byte id)
    {
        var resultado = await _objetoLN.ObtenerAsync(
            new TObjeto { IdObjeto = id });

        if (!resultado.Success)
            return NotFound(resultado);

        return Ok(resultado);
    }

    [HttpGet("Buscar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Buscar(string texto = "")
    {
        var resultado = await _objetoLN.BuscarAsync(
            new TObjeto
            {
                Nombre = texto
            });

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    // bando = PACMAN o FANTASMA
    [HttpGet("Bando/{bando}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ListarPorBando(string bando)
    {
        var resultado = await _objetoLN.ListarPorBandoAsync(bando);

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }
}
