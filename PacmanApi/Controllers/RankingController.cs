using Microsoft.AspNetCore.Mvc;
using PacmanApi.Extensiones;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RankingController : ControllerBase
{
    private readonly IRankingLN _rankingLN;
    private readonly IUsuarioLN _usuarioLN;

    public RankingController(IRankingLN rankingLN, IUsuarioLN usuarioLN)
    {
        _rankingLN = rankingLN;
        _usuarioLN = usuarioLN;
    }

    // Público: rol = PACMAN o FANTASMA
    [HttpGet("Listar/{rol}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar(string rol)
    {
        var resultado = await _rankingLN.ListarPorRolAsync(rol);

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    // Posición del usuario de la sesión en ese rol
    [HttpGet("MiPosicion/{rol}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> MiPosicion(string rol)
    {
        var sesion = await _usuarioLN.ValidarSesionAsync(Request.ObtenerToken());

        if (!sesion.Success || sesion.Data == null)
            return Unauthorized(sesion);

        var resultado = await _rankingLN.ObtenerPosicionAsync(
            sesion.Data.IdUsuario, rol);

        if (!resultado.Success)
            return NotFound(resultado);

        return Ok(resultado);
    }
}
