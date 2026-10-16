using Microsoft.AspNetCore.Mvc;
using PacmanApi.Extensiones;
using PacmanDominio.InterfazLN;

namespace PacmanApi.Controllers;

// El usuario sale del token de sesión: cada jugador solo ve su propio historial
[Route("api/[controller]")]
[ApiController]
public class HistorialController : ControllerBase
{
    private readonly IHistorialLN _historialLN;
    private readonly IUsuarioLN _usuarioLN;

    public HistorialController(IHistorialLN historialLN, IUsuarioLN usuarioLN)
    {
        _historialLN = historialLN;
        _usuarioLN = usuarioLN;
    }

    [HttpGet("Listar")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar()
    {
        var sesion = await _usuarioLN.ValidarSesionAsync(Request.ObtenerToken());

        if (!sesion.Success || sesion.Data == null)
            return Unauthorized(sesion);

        var resultado = await _historialLN.ListarPorUsuarioAsync(
            sesion.Data.IdUsuario);

        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    // Detalle por nivel de una partida del usuario de la sesión
    [HttpGet("Rondas/{idPartida:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Rondas(int idPartida)
    {
        var sesion = await _usuarioLN.ValidarSesionAsync(Request.ObtenerToken());

        if (!sesion.Success || sesion.Data == null)
            return Unauthorized(sesion);

        var resultado = await _historialLN.ListarRondasAsync(
            sesion.Data.IdUsuario, idPartida);

        if (!resultado.Success)
            return NotFound(resultado);

        return Ok(resultado);
    }

    [HttpGet("Progreso")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Progreso()
    {
        var sesion = await _usuarioLN.ValidarSesionAsync(Request.ObtenerToken());

        if (!sesion.Success || sesion.Data == null)
            return Unauthorized(sesion);

        var resultado = await _historialLN.ObtenerProgresoAsync(
            sesion.Data.IdUsuario);

        if (!resultado.Success)
            return NotFound(resultado);

        return Ok(resultado);
    }
}
