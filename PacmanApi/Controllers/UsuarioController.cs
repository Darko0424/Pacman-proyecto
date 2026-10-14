using Microsoft.AspNetCore.Mvc;
using PacmanApi.Extensiones;
using PacmanDominio.Clases;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioLN _usuarioLN;

    public UsuarioController(IUsuarioLN usuarioLN)
    {
        _usuarioLN = usuarioLN;
    }

    [HttpPost("Registrar")]
    public async Task<IActionResult> Registrar([FromBody] clsRegistroUsuario datos)
    {
        var resultado = await _usuarioLN.RegistrarAsync(datos);

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpPost("IniciarSesion")]
    public async Task<IActionResult> IniciarSesion([FromBody] clsCredenciales datos)
    {
        var resultado = await _usuarioLN.IniciarSesionAsync(datos);

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpGet("Sesion")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Sesion()
    {
        var resultado = await _usuarioLN.ValidarSesionAsync(Request.ObtenerToken());

        if (!resultado.Success)
            return Unauthorized(resultado);

        return Ok(resultado);
    }

    [HttpPost("CerrarSesion")]
    public async Task<IActionResult> CerrarSesion()
    {
        var resultado = await _usuarioLN.CerrarSesionAsync(Request.ObtenerToken());

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }
}
