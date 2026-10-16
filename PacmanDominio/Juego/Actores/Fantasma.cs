using PacmanDominio.Juego.Mapa;

namespace PacmanDominio.Juego.Actores;

public sealed class Fantasma : Entidad
{
    // Tras una bola de poder: un Pac-Man puede comerlo
    public TimeSpan AsustadoHasta { get; private set; }

    // Tras chocar con otro fantasma ("explosión"): no puede moverse
    public TimeSpan AturdidoHasta { get; private set; }

    public override Bando Bando
    {
        get
        {
            return Bando.Fantasma;
        }
    }

    // idUsuario null = fantasma controlado por la IA
    public Fantasma(byte idPersonaje, int? idUsuario, Casilla posicionInicial, int vidas)
        : base(idPersonaje, idUsuario, posicionInicial, vidas)
    {
        if (!CatalogoPersonajes.EsFantasma(idPersonaje))
        {
            throw new ArgumentOutOfRangeException(
                nameof(idPersonaje), "El personaje no es un fantasma del catálogo.");
        }
    }

    public bool EstaAsustado(TimeSpan tiempoSimulacion)
    {
        return PuedeParticipar && tiempoSimulacion < AsustadoHasta;
    }

    public bool EstaAturdido(TimeSpan tiempoSimulacion)
    {
        return PuedeParticipar && tiempoSimulacion < AturdidoHasta;
    }

    internal void Asustar(TimeSpan hasta)
    {
        AsustadoHasta = hasta;
    }

    internal void Aturdir(TimeSpan hasta)
    {
        AturdidoHasta = hasta;
    }

    // Al perder una vida reaparece normal
    internal void Recuperar()
    {
        AsustadoHasta = TimeSpan.Zero;
        AturdidoHasta = TimeSpan.Zero;
    }
}
