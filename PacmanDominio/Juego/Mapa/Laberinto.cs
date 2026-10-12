using PacmanDominio.Juego.Items;

namespace PacmanDominio.Juego.Mapa;

public sealed class Laberinto
{
    private readonly Dictionary<(int Fila, int Columna), Casilla> _casillas = new();
    private readonly Dictionary<(int Fila, int Columna), Punto> _puntos = new();

    public int Filas { get; }
    public int Columnas { get; }
    public IReadOnlyList<Casilla> Casillas { get; }
    public IReadOnlyList<Casilla> PuntosReaparicion { get; }
    public IReadOnlyList<Punto> Puntos { get; }

    public int BolasRestantes
    {
        get
        {
            return Puntos.Count(punto => !punto.Consumido);
        }
    }

    public int BolasConsumidas
    {
        get
        {
            return Puntos.Count - BolasRestantes;
        }
    }

    public Laberinto(
        int filas,
        int columnas,
        IEnumerable<Casilla> casillas,
        IEnumerable<Casilla> puntosReaparicion,
        IEnumerable<Punto> puntos)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(filas);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columnas);
        ArgumentNullException.ThrowIfNull(casillas);
        ArgumentNullException.ThrowIfNull(puntosReaparicion);
        ArgumentNullException.ThrowIfNull(puntos);

        Filas = filas;
        Columnas = columnas;

        RegistrarCasillas(casillas);
        Casillas = _casillas.Values.ToList().AsReadOnly();
        PuntosReaparicion = ValidarPuntosReaparicion(puntosReaparicion);

        RegistrarPuntos(puntos);
        Puntos = _puntos.Values.ToList().AsReadOnly();
    }

    public Casilla ObtenerCasilla(int fila, int columna)
    {
        if (!_casillas.TryGetValue((fila, columna), out var casilla))
        {
            throw new ArgumentException("La posición está fuera del laberinto.");
        }

        return casilla;
    }

    public bool EsTransitable(int fila, int columna)
    {
        return _casillas.TryGetValue((fila, columna), out var casilla) &&
            casilla.EsTransitable;
    }

    public bool EsPuntoReaparicion(int fila, int columna)
    {
        return PuntosReaparicion.Any(
            casilla => casilla.Fila == fila && casilla.Columna == columna);
    }

    internal bool ConsumirPunto(Casilla posicion)
    {
        if (!_puntos.TryGetValue((posicion.Fila, posicion.Columna), out var punto))
        {
            return false;
        }

        return punto.Consumir();
    }

    private void RegistrarCasillas(IEnumerable<Casilla> casillas)
    {
        foreach (var casilla in casillas)
        {
            ArgumentNullException.ThrowIfNull(casilla);

            if (casilla.Fila >= Filas || casilla.Columna >= Columnas)
            {
                throw new ArgumentException(
                    "Las casillas deben estar dentro del laberinto.",
                    nameof(casillas));
            }

            if (!_casillas.TryAdd((casilla.Fila, casilla.Columna), casilla))
            {
                throw new ArgumentException(
                    "No se pueden repetir las coordenadas de una casilla.",
                    nameof(casillas));
            }
        }

        if (_casillas.Count != (long)Filas * Columnas)
        {
            throw new ArgumentException(
                "El laberinto debe definir todas sus casillas, incluidos los muros.",
                nameof(casillas));
        }
    }

    private IReadOnlyList<Casilla> ValidarPuntosReaparicion(
        IEnumerable<Casilla> puntosReaparicion)
    {
        var posiciones = new List<Casilla>();
        var coordenadas = new HashSet<(int Fila, int Columna)>();

        foreach (var puntoReaparicion in puntosReaparicion)
        {
            ArgumentNullException.ThrowIfNull(puntoReaparicion);

            var casilla = ObtenerCasillaTransitable(puntoReaparicion);

            if (!coordenadas.Add((casilla.Fila, casilla.Columna)))
            {
                throw new ArgumentException(
                    "Los puntos de reaparición deben ser distintos.",
                    nameof(puntosReaparicion));
            }

            posiciones.Add(casilla);
        }

        if (posiciones.Count != ConfiguracionPartida.CantidadPuntosReaparicion)
        {
            throw new ArgumentException(
                "El laberinto debe tener exactamente seis puntos de reaparición.",
                nameof(puntosReaparicion));
        }

        return posiciones.AsReadOnly();
    }

    private void RegistrarPuntos(IEnumerable<Punto> puntos)
    {
        foreach (var punto in puntos)
        {
            ArgumentNullException.ThrowIfNull(punto);
            var casilla = ObtenerCasillaTransitable(punto.Posicion);

            if (punto.Consumido)
            {
                throw new ArgumentException(
                    "Las bolas de un nuevo laberinto deben estar disponibles.",
                    nameof(puntos));
            }

            if (!_puntos.TryAdd((casilla.Fila, casilla.Columna), punto))
            {
                throw new ArgumentException(
                    "Solo puede haber una bola por casilla.",
                    nameof(puntos));
            }
        }
    }

    private Casilla ObtenerCasillaTransitable(Casilla posicion)
    {
        var casilla = ObtenerCasilla(posicion.Fila, posicion.Columna);

        if (!casilla.EsTransitable)
        {
            throw new ArgumentException("La casilla del laberinto debe ser transitable.");
        }

        return casilla;
    }
}
