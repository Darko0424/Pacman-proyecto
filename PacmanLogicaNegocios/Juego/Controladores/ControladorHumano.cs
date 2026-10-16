using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;

namespace PacmanLogicaNegocios.Juego.Controladores;

// Guarda la última intención recibida del celular. La dirección se mantiene
// (como en el Pac-Man clásico, gira en cuanto puede); el botón de acción se
// consume una sola vez.
internal sealed class ControladorHumano : IControlador
{
    private Direccion _direccion = Direccion.Ninguna;
    private bool _usarObjeto;

    public void RecibirDireccion(Direccion direccion)
    {
        // Ninguna = seguir como va
        if (direccion != Direccion.Ninguna)
        {
            _direccion = direccion;
        }
    }

    public void SolicitarUsarObjeto()
    {
        _usarObjeto = true;
    }

    public DecisionControl ObtenerDecision(
        Entidad actor, EstadoPartida partida, TimeSpan tiempoSimulacion)
    {
        var usarObjeto = _usarObjeto;
        _usarObjeto = false;

        return new DecisionControl(_direccion, usarObjeto);
    }
}
