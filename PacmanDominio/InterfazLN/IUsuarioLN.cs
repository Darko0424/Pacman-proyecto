using PacmanDominio.Clases;
using PacmanDominio.EntidadesTipadas;
using PacmanUtilitarios;

namespace PacmanDominio.InterfazLN;

public interface IUsuarioLN
{
    Task<Respuesta<TUsuario>> RegistrarAsync(clsRegistroUsuario datos);
    Task<Respuesta<clsSesionIniciada>> IniciarSesionAsync(clsCredenciales datos);
    Task<Respuesta<TUsuario>> ValidarSesionAsync(string token);
    Task<Respuesta<bool>> CerrarSesionAsync(string token);
    Task<Respuesta<clsSesionIniciada>> VerificarCorreoAsync(clsVerificacionCorreo datos);
    Task<Respuesta<bool>> ReenviarCodigoAsync(clsSolicitudCodigo datos);
    Task<Respuesta<clsSugerenciasNombre>> SugerirNombresAsync(string nombreDeseado);
    Task<Respuesta<TUsuario>> AsignarNombreAsync(string token, clsNombreUsuario datos);
    Task<Respuesta<bool>> SolicitarRecuperacionAsync(clsSolicitudCodigo datos);
    Task<Respuesta<clsSesionIniciada>> RestablecerContrasenaAsync(clsRestablecerContrasena datos);
    Task<Respuesta<bool>> SolicitarCambioContrasenaAsync(string token);
    Task<Respuesta<clsSesionIniciada>> CambiarContrasenaAsync(string token, clsCambioContrasena datos);
}
