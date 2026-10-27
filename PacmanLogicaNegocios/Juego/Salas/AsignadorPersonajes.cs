using PacmanDominio.Juego;
using PacmanDominio.Juego.Actores;
using PacmanDominio.Juego.Salas;
using PacmanLogicaNegocios.Juego.Sistemas;

namespace PacmanLogicaNegocios.Juego.Salas;

public sealed class AsignadorPersonajes
{
    private readonly Random _azar;
    private readonly object _sorteo = new();

    public AsignadorPersonajes(Random? azar = null)
    {
        _azar = azar ?? Random.Shared;
    }

    // La sala y el nivel deben pertenecer a la misma secuencia de operaciones.
    public EstadoPartida CrearNivel(Sala sala, NivelCargado nivel)
    {
        ArgumentNullException.ThrowIfNull(sala);
        ArgumentNullException.ThrowIfNull(nivel);

        if (sala.Vacia || nivel.Numero == null)
        {
            throw new ArgumentException("Se requiere una sala con jugadores y un nivel de catálogo.");
        }

        var modo = sala.Modo ?? sala.ModoSegunJugadores;
        var usuarios = SortearUsuarios(sala, modo);
        var pacmans = new List<PacMan>();
        var fantasmas = new List<Fantasma>();
        byte ultimoPacMan = modo == ModoPartida.ContraIa ? (byte)1 : (byte)2;

        for (byte idPersonaje = 1; idPersonaje <= 5; idPersonaje++)
        {
            if (idPersonaje == 2 && ultimoPacMan == 1)
            {
                continue;
            }

            usuarios.TryGetValue(idPersonaje, out var idUsuario);
            var posicion = nivel.PosicionesIniciales[idPersonaje];

            if (CatalogoPersonajes.EsPacMan(idPersonaje))
            {
                pacmans.Add(new PacMan(idPersonaje, idUsuario, posicion, sala.Configuracion.Vidas));
            }
            else
            {
                fantasmas.Add(new Fantasma(idPersonaje, idUsuario, posicion, sala.Configuracion.Vidas,
                    vidasInfinitas: sala.Configuracion.ModoClasico));
            }
        }

        return new EstadoPartida(
            nivel.Numero.Value, modo, sala.Configuracion, nivel.Laberinto, pacmans, fantasmas);
    }

    private Dictionary<byte, int?> SortearUsuarios(Sala sala, ModoPartida modo)
    {
        var usuarios = new Dictionary<byte, int?>();

        lock (_sorteo)
        {
            if (modo == ModoPartida.ContraIa)
            {
                var jugador = sala.Jugadores.Single();
                var rol = sala.RolContraIa ?? jugador.RolPreferido;
                rol ??= _azar.Next(2) == 0 ? Bando.PacMan : Bando.Fantasma;
                byte personaje = 1;

                if (rol == Bando.Fantasma)
                {
                    personaje = CatalogoPersonajes.IdsFantasmas[_azar.Next(3)];
                }

                usuarios.Add(personaje, jugador.IdUsuario);
            }
            else if (sala.Configuracion.BandosFijos &&
                sala.Estado == EstadoSala.Transicion &&
                sala.Jugadores.All(jugador => jugador.IdPersonaje.HasValue))
            {
                // Bandos fijos: cada uno repite el personaje del nivel anterior
                foreach (var jugador in sala.Jugadores)
                {
                    usuarios.Add(jugador.IdPersonaje!.Value, jugador.IdUsuario);
                }
            }
            else
            {
                var personajes = new byte[] { 1, 2, 3, 4 };
                _azar.Shuffle(personajes);

                for (int indice = 0; indice < sala.Jugadores.Count; indice++)
                {
                    usuarios.Add(personajes[indice], sala.Jugadores[indice].IdUsuario);
                }
            }
        }

        return usuarios;
    }
}
