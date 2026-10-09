using PacmanDominio.Entidades;
using PacmanDominio.Entidades.Vistas;

namespace PacmanDominio.InterfazAD
{
    public interface IUnidadTrabajoEF : IDisposable
    {
        IRepositorioAD<Dificultad> TDificultad { get; }
        IRepositorioAD<Fruta> TFruta { get; }
        IRepositorioAD<Nivel> TNivel { get; }
        IRepositorioAD<ParticipantePartida> TParticipantePartida { get; }
        IRepositorioAD<Partida> TPartida { get; }
        IRepositorioAD<Personaje> TPersonaje { get; }
        IRepositorioAD<SesionMovil> TSesionMovil { get; }
        IRepositorioAD<Usuario> TUsuario { get; }

        // Vistas: solo lectura (Listar, Buscar, ObtenerEntidad, Contar)
        IRepositorioAD<HistorialEntrada> VHistorialUsuario { get; }
        IRepositorioAD<ProgresoJugador> VProgresoJugador { get; }
        IRepositorioAD<RankingEntrada> VRanking { get; }

        int Completar();
        void CompletarTran();
        void EmpezarTransaccion();
        void Rollback();
        void CerrarConexion();
    }
}
