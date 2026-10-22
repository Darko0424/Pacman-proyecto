using PacmanDominio.EntidadesTipadas;
using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Interfaces;
using PacmanDominio.Juego.Items;
using PacmanDominio.Juego.Mapa;

namespace PacmanLogicaNegocios.Juego;

// Lo que comparten los sistemas del motor durante un nivel.
internal sealed class ContextoMotor
{
    public EstadoPartida Partida { get; }
    public TDificultad Dificultad { get; }
    public double VelocidadNivel { get; }
    public IReadOnlyDictionary<string, IEfectoObjeto> Efectos { get; }

    // Solo los objetos cuyo efecto tiene implementación
    public IReadOnlyList<TObjeto> Objetos { get; }

    public Random Azar { get; }

    // Cuándo perdió su última vida cada personaje, para la demora de reaparición
    public Dictionary<byte, TimeSpan> Muertes { get; } = new();

    public List<EventoJuego> Eventos { get; } = new();

    public int UltimoIdEfecto { get; set; }
    public int UltimoIdCaja { get; set; }
    public TimeSpan? ProximaCaja { get; set; }

    public TimeSpan Tiempo
    {
        get
        {
            return Partida.TiempoSimulacion;
        }
    }

    public Laberinto Laberinto
    {
        get
        {
            return Partida.Laberinto;
        }
    }

    public ContextoMotor(
        EstadoPartida partida,
        TDificultad dificultad,
        double velocidadNivel,
        IReadOnlyDictionary<string, IEfectoObjeto> efectos,
        IReadOnlyList<TObjeto> objetos,
        Random azar)
    {
        Partida = partida;
        Dificultad = dificultad;
        VelocidadNivel = velocidadNivel;
        Efectos = efectos;
        Objetos = objetos;
        Azar = azar;
    }

    public IEnumerable<(EfectoActivo Efecto, IEfectoObjeto Implementacion)> EfectosVigentes()
    {
        foreach (var efecto in Partida.EfectosActivos)
        {
            if (efecto.EstaVigente(Tiempo) &&
                Efectos.TryGetValue(efecto.TipoEfecto, out var implementacion))
            {
                yield return (efecto, implementacion);
            }
        }
    }

    public bool EstaProtegido(Entidad actor)
    {
        return EfectosVigentes().Any(vigente =>
            vigente.Implementacion.Protege(Partida, vigente.Efecto, actor));
    }

    // Puntos que gana el actor con los efectos vigentes (por ejemplo, puntos dobles)
    public int Puntos(Entidad actor, int puntos)
    {
        foreach (var (efecto, implementacion) in EfectosVigentes())
        {
            puntos *= implementacion.MultiplicadorPuntos(Partida, efecto, actor);
        }

        return puntos;
    }

    public void Registrar(EventoJuego evento)
    {
        Eventos.Add(evento);
    }
}
