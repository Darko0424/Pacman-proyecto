namespace PacmanDominio.Juego.Salas;

public sealed class JugadorConectado
{
    public int IdUsuario { get; }
    public string IdConexion { get; private set; }
    public bool EstaConectado { get; private set; }
    public byte? IdPersonaje { get; private set; }

    // La sala controla el ingreso y todas las modificaciones del jugador.
    internal JugadorConectado(int idUsuario, string idConexion)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);

        IdUsuario = idUsuario;
        IdConexion = idConexion;
        EstaConectado = true;
    }

    internal void AsignarPersonaje(byte? idPersonaje)
    {
        IdPersonaje = idPersonaje;
    }

    internal void Desconectar()
    {
        EstaConectado = false;
    }

    internal void Reconectar(string idConexion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);

        IdConexion = idConexion;
        EstaConectado = true;
    }
}
