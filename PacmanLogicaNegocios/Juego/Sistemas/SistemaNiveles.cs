using System.Collections.ObjectModel;
using System.Text.Json;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.Juego;
using PacmanDominio.Juego.Items;
using PacmanDominio.Juego.Mapa;

namespace PacmanLogicaNegocios.Juego.Sistemas;

public sealed class SistemaNiveles
{
    public const int VersionSoportada = 1;

    public NivelCargado Cargar(TNivel nivel)
    {
        ArgumentNullException.ThrowIfNull(nivel);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(nivel.IdNivel);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(nivel.Numero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(nivel.VelocidadBase);
        ArgumentException.ThrowIfNullOrWhiteSpace(nivel.Nombre);

        var definicion = Convertir(nivel.LaberintoJson);

        return new NivelCargado(
            definicion.Laberinto,
            definicion.PosicionesIniciales,
            nivel);
    }

    // Cada llamada construye casillas y bolas propias de la nueva partida.
    public NivelCargado Convertir(string laberintoJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(laberintoJson);

        using var documento = LeerDocumento(laberintoJson);
        var raiz = documento.RootElement;

        ValidarPropiedades(
            raiz, "laberinto", "version", "filas", "columnas", "casillas",
            "puntosReaparicion", "posicionesIniciales");

        int version = LeerEntero(raiz, "version", "laberinto");

        if (version != VersionSoportada)
        {
            throw new FormatException(
                $"La versión {version} del laberinto no está soportada; se requiere la versión 1.");
        }

        int filas = LeerEntero(raiz, "filas", "laberinto");
        int columnas = LeerEntero(raiz, "columnas", "laberinto");

        if (filas <= 0 || columnas <= 0)
        {
            throw new FormatException("Las dimensiones filas y columnas deben ser mayores que cero.");
        }

        var matriz = LeerMatriz(raiz, filas, columnas);
        var casillas = new List<Casilla>();
        var puntos = new List<Punto>();

        ConstruirCasillas(matriz, casillas, puntos);

        var puntosReaparicion = LeerPuntosReaparicion(raiz, casillas, filas, columnas);
        var posicionesIniciales = LeerPosicionesIniciales(raiz, casillas, filas, columnas);
        var laberinto = new Laberinto(filas, columnas, casillas, puntosReaparicion, puntos);

        ValidarConectividad(laberinto);

        return new NivelCargado(laberinto, posicionesIniciales);
    }

    private static JsonDocument LeerDocumento(string laberintoJson)
    {
        try
        {
            return JsonDocument.Parse(laberintoJson);
        }
        catch (JsonException excepcion)
        {
            throw new FormatException(
                $"El JSON del laberinto está mal formado: {excepcion.Message}",
                excepcion);
        }
    }

    private static List<string> LeerMatriz(JsonElement raiz, int filas, int columnas)
    {
        var elementos = LeerArreglo(raiz, "casillas");

        if (elementos.GetArrayLength() != filas)
        {
            throw new FormatException($"casillas debe contener exactamente {filas} filas de texto.");
        }

        var matriz = new List<string>();

        foreach (var elemento in elementos.EnumerateArray())
        {
            int fila = matriz.Count;

            if (elemento.ValueKind != JsonValueKind.String)
            {
                throw new FormatException($"casillas[{fila}] debe ser una fila de texto.");
            }

            string texto = elemento.GetString()!;

            if (texto.Length != columnas)
            {
                throw new FormatException($"casillas[{fila}] debe tener {columnas} columnas; tiene {texto.Length}.");
            }

            matriz.Add(texto);
        }

        return matriz;
    }

    private static void ConstruirCasillas(
        IReadOnlyList<string> matriz,
        List<Casilla> casillas,
        List<Punto> puntos)
    {
        int bolasNormales = 0;
        int bolasPoder = 0;

        for (int fila = 0; fila < matriz.Count; fila++)
        {
            for (int columna = 0; columna < matriz[fila].Length; columna++)
            {
                char simbolo = matriz[fila][columna];

                if (simbolo != '#' && simbolo != '.' && simbolo != 'o' && simbolo != ' ')
                {
                    throw new FormatException($"Símbolo '{simbolo}' no soportado en casillas[{fila}][{columna}].");
                }

                var casilla = new Casilla(fila, columna, simbolo != '#');
                casillas.Add(casilla);

                if (simbolo == '.')
                {
                    puntos.Add(new Punto(casilla));
                    bolasNormales++;
                }
                else if (simbolo == 'o')
                {
                    puntos.Add(new PuntoPoder(casilla));
                    bolasPoder++;
                }
            }
        }

        if (bolasNormales == 0 || bolasPoder == 0)
        {
            throw new FormatException("El laberinto debe contener bolas normales (.) y bolas de poder (o).");
        }
    }

    private static List<Casilla> LeerPuntosReaparicion(
        JsonElement raiz,
        IReadOnlyList<Casilla> casillas,
        int filas,
        int columnas)
    {
        var elementos = LeerArreglo(raiz, "puntosReaparicion");

        if (elementos.GetArrayLength() != ConfiguracionPartida.CantidadPuntosReaparicion)
        {
            throw new FormatException("puntosReaparicion debe contener exactamente seis posiciones.");
        }

        var posiciones = new List<Casilla>();
        var coordenadas = new HashSet<(int Fila, int Columna)>();

        foreach (var elemento in elementos.EnumerateArray())
        {
            string contexto = $"puntosReaparicion[{posiciones.Count}]";
            ValidarPropiedades(elemento, contexto, "fila", "columna");
            var casilla = LeerPosicion(elemento, contexto, casillas, filas, columnas);

            if (!coordenadas.Add((casilla.Fila, casilla.Columna)))
            {
                throw new FormatException($"{contexto} repite una posición de reaparición.");
            }

            posiciones.Add(casilla);
        }

        return posiciones;
    }

    private static Dictionary<byte, Casilla> LeerPosicionesIniciales(
        JsonElement raiz,
        IReadOnlyList<Casilla> casillas,
        int filas,
        int columnas)
    {
        var elementos = LeerArreglo(raiz, "posicionesIniciales");

        if (elementos.GetArrayLength() != 5)
        {
            throw new FormatException("posicionesIniciales debe contener los cinco personajes del catálogo (1 a 5).");
        }

        var posiciones = new Dictionary<byte, Casilla>();
        var coordenadas = new HashSet<(int Fila, int Columna)>();

        foreach (var elemento in elementos.EnumerateArray())
        {
            string contexto = $"posicionesIniciales[{posiciones.Count}]";
            ValidarPropiedades(elemento, contexto, "idPersonaje", "fila", "columna");
            int idPersonaje = LeerEntero(elemento, "idPersonaje", contexto);

            if (idPersonaje < 1 || idPersonaje > 5)
            {
                throw new FormatException($"{contexto}.idPersonaje debe pertenecer al catálogo: 1 a 5.");
            }

            var casilla = LeerPosicion(elemento, contexto, casillas, filas, columnas);

            if (!posiciones.TryAdd((byte)idPersonaje, casilla))
            {
                throw new FormatException($"El personaje {idPersonaje} tiene más de una posición inicial.");
            }

            if (!coordenadas.Add((casilla.Fila, casilla.Columna)))
            {
                throw new FormatException($"{contexto} comparte su posición inicial con otro personaje.");
            }
        }

        return posiciones;
    }

    private static Casilla LeerPosicion(
        JsonElement elemento,
        string contexto,
        IReadOnlyList<Casilla> casillas,
        int filas,
        int columnas)
    {
        int fila = LeerEntero(elemento, "fila", contexto);
        int columna = LeerEntero(elemento, "columna", contexto);

        if (fila < 0 || fila >= filas || columna < 0 || columna >= columnas)
        {
            throw new FormatException($"{contexto}: la posición ({fila}, {columna}) está fuera del laberinto.");
        }

        var casilla = casillas[fila * columnas + columna];

        if (!casilla.EsTransitable)
        {
            throw new FormatException($"{contexto}: la posición ({fila}, {columna}) está en un muro.");
        }

        return casilla;
    }

    private static void ValidarConectividad(Laberinto laberinto)
    {
        var origen = laberinto.PuntosReaparicion[0];
        var pendientes = new Queue<Casilla>();
        var visitadas = new HashSet<(int Fila, int Columna)>();
        var direcciones = new (int Fila, int Columna)[]
        {
            (-1, 0),
            (1, 0),
            (0, -1),
            (0, 1)
        };

        pendientes.Enqueue(origen);
        visitadas.Add((origen.Fila, origen.Columna));

        while (pendientes.Count > 0)
        {
            var casilla = pendientes.Dequeue();

            foreach (var direccion in direcciones)
            {
                int fila = casilla.Fila + direccion.Fila;
                int columna = casilla.Columna + direccion.Columna;

                if (laberinto.EsTransitable(fila, columna) && visitadas.Add((fila, columna)))
                {
                    pendientes.Enqueue(laberinto.ObtenerCasilla(fila, columna));
                }
            }
        }

        var inaccesible = laberinto.Casillas.FirstOrDefault(
            casilla => casilla.EsTransitable && !visitadas.Contains((casilla.Fila, casilla.Columna)));

        if (inaccesible != null)
        {
            throw new FormatException(
                "El laberinto tiene zonas desconectadas; " +
                $"no se alcanza ({inaccesible.Fila}, {inaccesible.Columna}) mediante movimientos cardinales.");
        }
    }

    private static void ValidarPropiedades(
        JsonElement elemento,
        string contexto,
        params string[] esperadas)
    {
        if (elemento.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException($"{contexto} debe ser un objeto JSON.");
        }

        var presentes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var propiedad in elemento.EnumerateObject())
        {
            if (!presentes.Add(propiedad.Name))
            {
                throw new FormatException($"{contexto}: la propiedad '{propiedad.Name}' está repetida.");
            }

            if (!esperadas.Contains(propiedad.Name))
            {
                throw new FormatException($"{contexto}: la propiedad '{propiedad.Name}' no está soportada.");
            }
        }

        foreach (string nombre in esperadas)
        {
            if (!presentes.Contains(nombre))
            {
                throw new FormatException($"Falta la propiedad obligatoria {contexto}.{nombre}.");
            }
        }
    }

    private static int LeerEntero(JsonElement elemento, string nombre, string contexto)
    {
        var propiedad = elemento.GetProperty(nombre);

        if (propiedad.ValueKind != JsonValueKind.Number || !propiedad.TryGetInt32(out int valor))
        {
            throw new FormatException($"{contexto}.{nombre} debe ser un entero de 32 bits.");
        }

        return valor;
    }

    private static JsonElement LeerArreglo(JsonElement raiz, string nombre)
    {
        var propiedad = raiz.GetProperty(nombre);

        if (propiedad.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException($"{nombre} debe ser un arreglo JSON.");
        }

        return propiedad;
    }
}

// Conserva los metadatos por valor; nunca retiene el TNivel modificable del catálogo.
public sealed class NivelCargado
{
    public int Version { get; } = SistemaNiveles.VersionSoportada;
    public int? IdNivel { get; }
    public int? Numero { get; }
    public string? Nombre { get; }
    public decimal? VelocidadBase { get; }
    public Laberinto Laberinto { get; }
    public IReadOnlyDictionary<byte, Casilla> PosicionesIniciales { get; }

    internal NivelCargado(
        Laberinto laberinto,
        IReadOnlyDictionary<byte, Casilla> posicionesIniciales,
        TNivel? nivel = null)
    {
        Laberinto = laberinto;
        PosicionesIniciales = new ReadOnlyDictionary<byte, Casilla>(
            new Dictionary<byte, Casilla>(posicionesIniciales));
        IdNivel = nivel?.IdNivel;
        Numero = nivel?.Numero;
        Nombre = nivel?.Nombre;
        VelocidadBase = nivel?.VelocidadBase;
    }
}
