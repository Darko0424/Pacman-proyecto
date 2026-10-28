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

    [HttpPost("VerificarCorreo")]
    public async Task<IActionResult> VerificarCorreo([FromBody] clsVerificacionCorreo datos)
    {
        var resultado = await _usuarioLN.VerificarCorreoAsync(datos);

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpPost("ReenviarCodigo")]
    public async Task<IActionResult> ReenviarCodigo([FromBody] clsSolicitudCodigo datos)
    {
        var resultado = await _usuarioLN.ReenviarCodigoAsync(datos);

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

    [HttpGet("SugerirNombres")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> SugerirNombres([FromQuery] string nombre)
    {
        var resultado = await _usuarioLN.SugerirNombresAsync(nombre);

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpPost("AsignarNombre")]
    public async Task<IActionResult> AsignarNombre([FromBody] clsNombreUsuario datos)
    {
        var resultado = await _usuarioLN.AsignarNombreAsync(Request.ObtenerToken(), datos);

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpPost("SolicitarRecuperacion")]
    public async Task<IActionResult> SolicitarRecuperacion([FromBody] clsSolicitudCodigo datos)
    {
        var resultado = await _usuarioLN.SolicitarRecuperacionAsync(datos);

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpPost("RestablecerContrasena")]
    public async Task<IActionResult> RestablecerContrasena([FromBody] clsRestablecerContrasena datos)
    {
        var resultado = await _usuarioLN.RestablecerContrasenaAsync(datos);

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpPost("SolicitarCambioContrasena")]
    public async Task<IActionResult> SolicitarCambioContrasena()
    {
        var resultado = await _usuarioLN.SolicitarCambioContrasenaAsync(Request.ObtenerToken());

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpPost("CambiarContrasena")]
    public async Task<IActionResult> CambiarContrasena([FromBody] clsCambioContrasena datos)
    {
        var resultado = await _usuarioLN.CambiarContrasenaAsync(Request.ObtenerToken(), datos);

        if (!resultado.Success)
            return BadRequest(resultado);

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
