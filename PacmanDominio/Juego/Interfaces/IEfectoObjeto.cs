using PacmanDominio.Juego.Items;

namespace PacmanDominio.Juego.Interfaces;

// Una implementación por cada clave de objetos.efecto. Los objetos definitivos
// aún no están decididos: agregar uno es crear su clase y su fila en el catálogo.
public interface IEfectoObjeto
{
    // Clave del catálogo, por ejemplo VELOCIDAD, ESCUDO o CONGELAR
    string TipoEfecto { get; }

    // Se llama una vez al usar el objeto; el efecto ya está en EfectosActivos
    void Aplicar(EstadoPartida partida, EfectoActivo efecto);

    // Se llama cuando vence, antes de quitarlo de EfectosActivos
    void Retirar(EstadoPartida partida, EfectoActivo efecto);
}
