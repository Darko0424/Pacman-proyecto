using PacmanDominio.Entidades;
using PacmanDominio.Entidades.Vistas;

namespace PacmanDominio.InterfazAD
{
    public interface IUnidadTrabajoEF : IDisposable
    {
        IRepositorioAD<Dificultad> TDificultad { get; }
        IRepositorioAD<Nivel> TNivel { get; }
        IRepositorioAD<Objeto> TObjeto { get; }
        IRepositorioAD<ParticipanteRonda> TParticipanteRonda { get; }
        IRepositorioAD<Partida> TPartida { get; }
        IRepositorioAD<Personaje> TPersonaje { get; }
        IRepositorioAD<Ronda> TRonda { get; }
        IRepositorioAD<SesionMovil> TSesionMovil { get; }
        IRepositorioAD<Usuario> TUsuario { get; }

        // Vistas: solo lectura (Listar, Buscar, ObtenerEntidad, Contar)
        IRepositorioAD<HistorialEntrada> VHistorialUsuario { get; }
        IRepositorioAD<HistorialRonda> VHistorialRondas { get; }
        IRepositorioAD<ProgresoJugador> VProgresoJugador { get; }
        IRepositorioAD<RankingEntrada> VRanking { get; }
        IRepositorioAD<RankingTotalEntrada> VRankingTotal { get; }

        int Completar();
        void CompletarTran();
        void EmpezarTransaccion();
        void Rollback();
        void CerrarConexion();
    }
}
