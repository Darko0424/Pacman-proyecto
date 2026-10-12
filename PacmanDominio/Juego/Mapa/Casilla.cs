namespace PacmanDominio.Juego.Mapa;

public sealed class Casilla
{
    public int Fila { get; }
    public int Columna { get; }
    public bool EsTransitable { get; }

    public Casilla(int fila, int columna, bool esTransitable)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fila);
        ArgumentOutOfRangeException.ThrowIfNegative(columna);

        Fila = fila;
        Columna = columna;
        EsTransitable = esTransitable;
    }
}
