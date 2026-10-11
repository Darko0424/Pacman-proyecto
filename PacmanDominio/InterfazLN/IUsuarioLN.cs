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
}
