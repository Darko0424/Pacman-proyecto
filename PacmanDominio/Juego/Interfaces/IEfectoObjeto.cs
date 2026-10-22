using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Items;

namespace PacmanDominio.Juego.Interfaces;

// Una implementación por cada clave de objetos.efecto. Los objetos definitivos
// aún no están decididos: agregar uno es crear su clase, registrarla en el motor
// y agregar su fila al catálogo. Solo hace falta implementar lo que el efecto use.
public interface IEfectoObjeto
{
    // Clave del catálogo, por ejemplo VELOCIDAD, ESCUDO o CONGELAR
    string TipoEfecto { get; }

    // Se llama una vez al usar el objeto; el efecto ya está en EfectosActivos.
    // Para efectos instantáneos (por ejemplo, teletransportar).
    void Aplicar(EstadoPartida partida, EfectoActivo efecto)
    {
    }

    // Se llama cuando vence, antes de quitarlo de EfectosActivos
    void Retirar(EstadoPartida partida, EfectoActivo efecto)
    {
    }

    // Mientras está vigente: factor sobre la velocidad de cada actor (1 = sin cambio, 0 = inmóvil)
    double MultiplicadorVelocidad(EstadoPartida partida, EfectoActivo efecto, Entidad actor)
    {
        return 1;
    }

    // Mientras está vigente: factor sobre los puntos que gana cada actor (1 = sin cambio)
    int MultiplicadorPuntos(EstadoPartida partida, EfectoActivo efecto, Entidad actor)
    {
        return 1;
    }

    // Mientras está vigente: true si el actor no puede ser atrapado ni comido
    bool Protege(EstadoPartida partida, EfectoActivo efecto, Entidad actor)
    {
        return false;
    }
}
