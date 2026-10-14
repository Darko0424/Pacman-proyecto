using AutoMapper;
using Microsoft.Extensions.Logging;
using PacmanDominio.Entidades;
using PacmanDominio.EntidadesTipadas;
using PacmanDominio.InterfazAD;
using PacmanDominio.InterfazLN;
using PacmanDominio.Juego;
using PacmanUtilitarios;

namespace PacmanLogicaNegocios.Implementacion
{
    public class PartidaLN : BaseLN<PartidaLN>, IPartidaLN
    {
        // Mismos valores que los CHECK de la tabla partidas
        private const string ModoMultijugador = "MULTIJUGADOR";
        private const string ModoContraIa = "CONTRA_IA";
        private const string EstadoFinalizada = "FINALIZADA";
        private const string EstadoAbandonada = "ABANDONADA";
        private static readonly string[] BandosValidos = ["PACMAN", "FANTASMAS"];

        private const int LargoMaximoCodigoSala = 6;
        private const int CantidadMaximaParticipantes =
            ConfiguracionPartida.CantidadPacMan +
            ConfiguracionPartida.CantidadFantasmasHumanos +
            ConfiguracionPartida.CantidadFantasmasIa;

        public PartidaLN(
            IUnidadTrabajoEF unidadTrabajo,
            ILogger<PartidaLN> logger,
            IMapper mapper)
            : base(unidadTrabajo, mapper, logger)
        {
        }

        public async Task<Respuesta<TPartida>> GuardarResultadoAsync(
            TPartida partida, IReadOnlyCollection<TParticipantePartida> participantes)
        {
            var resultado = new Respuesta<TPartida>();

            try
            {
                if (partida == null || participantes == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Faltan los datos de la partida.";
                    return resultado;
                }

                partida.Modo = (partida.Modo ?? string.Empty).Trim().ToUpperInvariant();
                partida.Estado = (partida.Estado ?? string.Empty).Trim().ToUpperInvariant();
                partida.BandoGanador = string.IsNullOrWhiteSpace(partida.BandoGanador)
                    ? null
                    : partida.BandoGanador.Trim().ToUpperInvariant();
                partida.CodigoSala = string.IsNullOrWhiteSpace(partida.CodigoSala)
                    ? null
                    : partida.CodigoSala.Trim();

                var errorValidacion =
                    ValidarPartida(partida) ?? ValidarParticipantes(participantes);

                if (errorValidacion != null)
                {
                    resultado.Success = false;
                    resultado.Error = errorValidacion;
                    return resultado;
                }

                errorValidacion = await ValidarReferenciasAsync(partida, participantes);

                if (errorValidacion != null)
                {
                    resultado.Success = false;
                    resultado.Error = errorValidacion;
                    return resultado;
                }

                // Un reintento trae la misma sala, modo y fecha de inicio:
                // se devuelve lo ya guardado en vez de duplicarlo
                var partidaExistente =
                    await BuscarPartidaGuardadaAsync(partida, participantes);

                if (!partidaExistente.Success)
                {
                    resultado.Success = false;
                    resultado.Error = partidaExistente.Error;
                    return resultado;
                }

                if (partidaExistente.Data != null)
                {
                    resultado.Data = _mapper.Map<TPartida>(partidaExistente.Data);
                    return resultado;
                }

                var entidad = _mapper.Map<Partida>(partida);
                entidad.IdPartida = 0;

                _unidadDeTrabajo.EmpezarTransaccion();

                var respuestaPartida =
                    await _unidadDeTrabajo.TPartida.InsertarAsync(entidad);

                if (!respuestaPartida.Success || respuestaPartida.Data == null)
                {
                    _unidadDeTrabajo.Rollback();

                    resultado.Success = false;
                    resultado.Error = respuestaPartida.Error;
                    return resultado;
                }

                foreach (var participante in participantes)
                {
                    var entidadParticipante =
                        _mapper.Map<ParticipantePartida>(participante);
                    entidadParticipante.IdParticipante = 0;
                    entidadParticipante.IdPartida = respuestaPartida.Data.IdPartida;

                    var respuestaParticipante =
                        await _unidadDeTrabajo.TParticipantePartida.InsertarAsync(
                            entidadParticipante);

                    if (!respuestaParticipante.Success)
                    {
                        _unidadDeTrabajo.Rollback();

                        resultado.Success = false;
                        resultado.Error = respuestaParticipante.Error;
                        return resultado;
                    }
                }

                _unidadDeTrabajo.CompletarTran();

                resultado.Data = _mapper.Map<TPartida>(respuestaPartida.Data);
            }
            catch (Exception ex)
            {
                _unidadDeTrabajo.Rollback();

                _logger.LogError(
                    ex,
                    "Error al guardar resultado de la sala {CodigoSala}",
                    partida?.CodigoSala);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<TPartida>> ObtenerAsync(TPartida datos)
        {
            var resultado = new Respuesta<TPartida>();

            try
            {
                var respuestaRepositorio =
                    await _unidadDeTrabajo.TPartida.ObtenerEntidadAsync(
                        x => x.IdPartida == datos.IdPartida);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                if (respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Partida no encontrada.";
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<TPartida>(respuestaRepositorio.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener IdPartida {IdPartida}",
                    datos.IdPartida);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<IEnumerable<TParticipantePartida>>> ListarParticipantesAsync(
            int idPartida)
        {
            var resultado = new Respuesta<IEnumerable<TParticipantePartida>>();

            try
            {
                var partidaExistente =
                    await _unidadDeTrabajo.TPartida.ContarAsync(
                        x => x.IdPartida == idPartida);

                if (!partidaExistente.Success)
                {
                    resultado.Success = false;
                    resultado.Error = partidaExistente.Error;
                    return resultado;
                }

                if (partidaExistente.Data == 0)
                {
                    resultado.Success = false;
                    resultado.Error = "Partida no encontrada.";
                    return resultado;
                }

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TParticipantePartida.BuscarAsync(
                        x => x.IdPartida == idPartida);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TParticipantePartida>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.IdPersonaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al listar participantes de IdPartida {IdPartida}",
                    idPartida);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        private static string? ValidarPartida(TPartida partida)
        {
            if (partida.Modo != ModoMultijugador && partida.Modo != ModoContraIa)
                return "Modo no válido. Use MULTIJUGADOR o CONTRA_IA.";

            if (partida.Modo == ModoMultijugador && partida.CodigoSala == null)
                return "Una partida multijugador requiere el código de sala.";

            if (partida.Modo == ModoContraIa && partida.CodigoSala != null)
                return "Una partida contra la IA no lleva código de sala.";

            if (partida.CodigoSala?.Length > LargoMaximoCodigoSala)
                return "El código de sala admite como máximo seis caracteres.";

            // Solo se guardan partidas terminadas, nunca EN_CURSO
            if (partida.Estado != EstadoFinalizada && partida.Estado != EstadoAbandonada)
                return "Estado no válido. Use FINALIZADA o ABANDONADA.";

            if (partida.Estado == EstadoFinalizada &&
                !BandosValidos.Contains(partida.BandoGanador))
                return "Una partida finalizada requiere el bando ganador: PACMAN o FANTASMAS.";

            if (partida.Estado == EstadoAbandonada && partida.BandoGanador != null)
                return "Una partida abandonada no tiene bando ganador.";

            // La fecha de inicio la pone el motor: sirve para detectar reintentos
            if (partida.FechaInicio == default)
                return "Falta la fecha de inicio de la partida.";

            if (partida.FechaFin == null)
                return "Falta la fecha de fin de la partida.";

            if (partida.FechaFin < partida.FechaInicio)
                return "La fecha de fin no puede ser anterior a la de inicio.";

            return null;
        }

        private static string? ValidarParticipantes(
            IReadOnlyCollection<TParticipantePartida> participantes)
        {
            if (participantes.Count == 0 ||
                participantes.Count > CantidadMaximaParticipantes)
                return $"La partida debe tener entre 1 y {CantidadMaximaParticipantes} participantes.";

            if (participantes.Any(x => x == null))
                return "Hay un participante sin datos.";

            if (participantes.Any(x => x.EsIa != (x.IdUsuario == null)))
                return "La IA no tiene usuario y cada jugador humano debe tenerlo.";

            if (!participantes.Any(x => !x.EsIa))
                return "La partida debe tener al menos un jugador humano.";

            if (participantes.Select(x => x.IdPersonaje).Distinct().Count() !=
                participantes.Count)
                return "Un personaje no puede repetirse en la misma partida.";

            var usuarios = participantes
                .Where(x => x.IdUsuario != null)
                .Select(x => x.IdUsuario)
                .ToList();

            if (usuarios.Distinct().Count() != usuarios.Count)
                return "Un usuario no puede aparecer dos veces en la misma partida.";

            if (participantes.Any(x =>
                x.Puntuacion < 0 ||
                x.FantasmasComidos < 0 ||
                x.PacmansAtrapados < 0 ||
                x.FrutasComidas < 0))
                return "Las estadísticas de los participantes no pueden ser negativas.";

            return null;
        }

        private async Task<string?> ValidarReferenciasAsync(
            TPartida partida, IReadOnlyCollection<TParticipantePartida> participantes)
        {
            var dificultad = await _unidadDeTrabajo.TDificultad.ContarAsync(
                x => x.IdDificultad == partida.IdDificultad);

            if (!dificultad.Success)
                return dificultad.Error;

            if (dificultad.Data == 0)
                return "Dificultad no encontrada.";

            var nivel = await _unidadDeTrabajo.TNivel.ContarAsync(
                x => x.IdNivel == partida.IdNivelAlcanzado);

            if (!nivel.Success)
                return nivel.Error;

            if (nivel.Data == 0)
                return "Nivel no encontrado.";

            var idsPersonajes = participantes
                .Select(x => x.IdPersonaje)
                .ToList();

            var personajes = await _unidadDeTrabajo.TPersonaje.ContarAsync(
                x => idsPersonajes.Contains(x.IdPersonaje));

            if (!personajes.Success)
                return personajes.Error;

            if (personajes.Data != idsPersonajes.Count)
                return "Uno de los personajes no existe.";

            var idsUsuarios = participantes
                .Where(x => x.IdUsuario != null)
                .Select(x => x.IdUsuario!.Value)
                .ToList();

            var usuarios = await _unidadDeTrabajo.TUsuario.ContarAsync(
                x => idsUsuarios.Contains(x.IdUsuario));

            if (!usuarios.Success)
                return usuarios.Error;

            if (usuarios.Data != idsUsuarios.Count)
                return "Uno de los usuarios no existe.";

            return null;
        }

        private async Task<Respuesta<Partida>> BuscarPartidaGuardadaAsync(
            TPartida partida, IReadOnlyCollection<TParticipantePartida> participantes)
        {
            var candidatas = await _unidadDeTrabajo.TPartida.BuscarAsync(
                x => x.Modo == partida.Modo &&
                     x.CodigoSala == partida.CodigoSala &&
                     x.FechaInicio == partida.FechaInicio,
                [nameof(Partida.ParticipantesPartida)]);

            if (!candidatas.Success)
                return Respuesta<Partida>.Fail(candidatas.Error);

            // Sin código de sala (CONTRA_IA) también deben coincidir los jugadores
            var idsUsuarios = participantes
                .Where(x => x.IdUsuario != null)
                .Select(x => x.IdUsuario)
                .ToHashSet();

            return new Respuesta<Partida>
            {
                Data = (candidatas.Data ?? []).FirstOrDefault(x =>
                    partida.CodigoSala != null ||
                    x.ParticipantesPartida
                        .Where(p => p.IdUsuario != null)
                        .Select(p => p.IdUsuario)
                        .ToHashSet()
                        .SetEquals(idsUsuarios))
            };
        }
    }
}
