using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PacmanAccesoDatos.Contexto;
using PacmanDominio.Entidades;
using PacmanDominio.Entidades.Vistas;
using PacmanDominio.InterfazAD;

namespace PacmanAccesoDatos.Implementaciones
{
    public class UnidadTrabajoEF : IUnidadTrabajoEF
    {
        #region Atributos y Variables

        private PacManDbContext _Contexto { get; set; }

        public UnidadTrabajoEF(PacManDbContext Contexto)
        {
            _Contexto = Contexto;
        }

        private IDbContextTransaction? _transaction = null;

        private RepositorioAD<Dificultad>? _TDificultad;
        private RepositorioAD<Fruta>? _TFruta;
        private RepositorioAD<Nivel>? _TNivel;
        private RepositorioAD<ParticipantePartida>? _TParticipantePartida;
        private RepositorioAD<Partida>? _TPartida;
        private RepositorioAD<Personaje>? _TPersonaje;
        private RepositorioAD<SesionMovil>? _TSesionMovil;
        private RepositorioAD<Usuario>? _TUsuario;

        private RepositorioAD<HistorialEntrada>? _VHistorialUsuario;
        private RepositorioAD<ProgresoJugador>? _VProgresoJugador;
        private RepositorioAD<RankingEntrada>? _VRanking;

        #endregion

        #region Repositorios

        public IRepositorioAD<Dificultad> TDificultad
        {
            get
            {
                if (_TDificultad == null)
                    _TDificultad = new RepositorioAD<Dificultad>(_Contexto);

                return _TDificultad;
            }
        }

        public IRepositorioAD<Fruta> TFruta
        {
            get
            {
                if (_TFruta == null)
                    _TFruta = new RepositorioAD<Fruta>(_Contexto);

                return _TFruta;
            }
        }

        public IRepositorioAD<Nivel> TNivel
        {
            get
            {
                if (_TNivel == null)
                    _TNivel = new RepositorioAD<Nivel>(_Contexto);

                return _TNivel;
            }
        }

        public IRepositorioAD<ParticipantePartida> TParticipantePartida
        {
            get
            {
                if (_TParticipantePartida == null)
                    _TParticipantePartida =
                        new RepositorioAD<ParticipantePartida>(_Contexto);

                return _TParticipantePartida;
            }
        }

        public IRepositorioAD<Partida> TPartida
        {
            get
            {
                if (_TPartida == null)
                    _TPartida = new RepositorioAD<Partida>(_Contexto);

                return _TPartida;
            }
        }

        public IRepositorioAD<Personaje> TPersonaje
        {
            get
            {
                if (_TPersonaje == null)
                    _TPersonaje = new RepositorioAD<Personaje>(_Contexto);

                return _TPersonaje;
            }
        }

        public IRepositorioAD<SesionMovil> TSesionMovil
        {
            get
            {
                if (_TSesionMovil == null)
                    _TSesionMovil = new RepositorioAD<SesionMovil>(_Contexto);

                return _TSesionMovil;
            }
        }

        public IRepositorioAD<Usuario> TUsuario
        {
            get
            {
                if (_TUsuario == null)
                    _TUsuario = new RepositorioAD<Usuario>(_Contexto);

                return _TUsuario;
            }
        }

        public IRepositorioAD<HistorialEntrada> VHistorialUsuario
        {
            get
            {
                if (_VHistorialUsuario == null)
                    _VHistorialUsuario =
                        new RepositorioAD<HistorialEntrada>(_Contexto);

                return _VHistorialUsuario;
            }
        }

        public IRepositorioAD<ProgresoJugador> VProgresoJugador
        {
            get
            {
                if (_VProgresoJugador == null)
                    _VProgresoJugador =
                        new RepositorioAD<ProgresoJugador>(_Contexto);

                return _VProgresoJugador;
            }
        }

        public IRepositorioAD<RankingEntrada> VRanking
        {
            get
            {
                if (_VRanking == null)
                    _VRanking = new RepositorioAD<RankingEntrada>(_Contexto);

                return _VRanking;
            }
        }

        #endregion

        #region Métodos

        public int Completar()
        {
            return _Contexto.SaveChanges();
        }

        public void EmpezarTransaccion()
        {
            if (_transaction != null)
                throw new InvalidOperationException(
                    "Ya hay una transacción activa.");

            _transaction = _Contexto.Database.BeginTransaction();
        }

        public void CompletarTran()
        {
            if (_transaction == null)
                throw new InvalidOperationException(
                    "No hay una transacción activa.");

            try
            {
                _Contexto.SaveChanges();
                _transaction.Commit();
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Rollback()
        {
            if (_transaction == null)
                return;

            try
            {
                _transaction.Rollback();
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void CerrarConexion()
        {
            _Contexto.Database.CloseConnection();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _Contexto.Dispose();
        }

        #endregion
    }
}
