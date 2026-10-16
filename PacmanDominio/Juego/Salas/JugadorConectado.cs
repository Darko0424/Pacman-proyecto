namespace PacmanDominio.Juego.Salas;

public sealed class JugadorConectado
{
    public int IdUsuario { get; }
    public string NombreUsuario { get; }
    public string IdConexion { get; private set; }
    public bool EstaConectado { get; private set; }

    // Personaje del nivel en juego; cambia en cada nivel porque los roles se sortean
    public byte? IdPersonaje { get; private set; }

    // Solo cuenta si juega solo contra la IA; null = que decida el sorteo
    public Bando? RolPreferido { get; private set; }

    // La sala controla el ingreso y todas las modificaciones del jugador.
    internal JugadorConectado(int idUsuario, string nombreUsuario, string idConexion)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idUsuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreUsuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(idConexion);

        IdUsuario = idUsuario;
        NombreUsuario = nombreUsuario;
        IdConexion = idConexion;
        EstaConectado = true;
    }

    internal void AsignarPersonaje(byte? idPersonaje)
    {
        IdPersonaje = idPersonaje;
    }

    internal void ElegirRol(Bando? rol)
    {
        RolPreferido = rol;
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
