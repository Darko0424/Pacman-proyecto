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
    // La serie se guarda por partes mientras se juega: inicio, cada nivel y cierre.
    public class PartidaLN : BaseLN<PartidaLN>, IPartidaLN
    {
        // Mismos valores que los CHECK de la tabla partidas
        private const string EstadoEnCurso = "EN_CURSO";
        private const string EstadoFinalizada = "FINALIZADA";
        private const string EstadoAbandonada = "ABANDONADA";

        private const int LargoMaximoCodigoSala = 6;

        public PartidaLN(
            IUnidadTrabajoEF unidadTrabajo,
            ILogger<PartidaLN> logger,
            IMapper mapper)
            : base(unidadTrabajo, mapper, logger)
        {
        }

        public async Task<Respuesta<TPartida>> IniciarAsync(TPartida partida)
        {
            var resultado = new Respuesta<TPartida>();

            try
            {
                if (partida == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Faltan los datos de la partida.";
                    return resultado;
                }

                partida.Modo = (partida.Modo ?? string.Empty).Trim().ToUpperInvariant();
                partida.CodigoSala = (partida.CodigoSala ?? string.Empty).Trim();

                var errorValidacion = ValidarInicio(partida);

                if (errorValidacion != null)
                {
                    resultado.Success = false;
                    resultado.Error = errorValidacion;
                    return resultado;
                }

                var dificultad = await _unidadDeTrabajo.TDificultad.ContarAsync(
                    x => x.IdDificultad == partida.IdDificultad);

                if (!dificultad.Success)
                {
                    resultado.Success = false;
                    resultado.Error = dificultad.Error;
                    return resultado;
                }

                if (dificultad.Data == 0)
                {
                    resultado.Success = false;
                    resultado.Error = "Dificultad no encontrada.";
                    return resultado;
                }

                // Un reintento trae la misma sala y fecha de inicio:
                // se devuelve lo ya registrado en vez de duplicarlo
                var partidaExistente =
                    await _unidadDeTrabajo.TPartida.ObtenerEntidadAsync(
                        x => x.CodigoSala == partida.CodigoSala &&
                             x.FechaInicio == partida.FechaInicio);

                if (!partidaExistente.Success)
                {
                    resultado.Success = false;
                    resultado.Error = partidaExistente.Error;
                    return resultado;
                }

                if (partidaExistente.Data != null)
                {
                    if (partidaExistente.Data.Modo != partida.Modo ||
                        partidaExistente.Data.IdDificultad != partida.IdDificultad)
                    {
                        resultado.Success = false;
                        resultado.Error =
                            "La sala ya registró otra partida con esa fecha de inicio.";
                        return resultado;
                    }

                    resultado.Data = _mapper.Map<TPartida>(partidaExistente.Data);
                    return resultado;
                }

                var entidad = _mapper.Map<Partida>(partida);
                entidad.IdPartida = 0;
                entidad.Estado = EstadoEnCurso;
                entidad.FechaFin = null;

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TPartida.InsertarAsync(entidad);

                if (!respuestaRepositorio.Success || respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data = _mapper.Map<TPartida>(respuestaRepositorio.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al iniciar la partida de la sala {CodigoSala}",
                    partida?.CodigoSala);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<TRonda>> GuardarRondaAsync(
            TRonda ronda, IReadOnlyCollection<TParticipanteRonda> participantes)
        {
            var resultado = new Respuesta<TRonda>();

            try
            {
                if (ronda == null || participantes == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Faltan los datos de la ronda.";
                    return resultado;
                }

                ronda.BandoGanador = string.IsNullOrWhiteSpace(ronda.BandoGanador)
                    ? null
                    : ronda.BandoGanador.Trim().ToUpperInvariant();

                var partida = await _unidadDeTrabajo.TPartida.ObtenerEntidadAsync(
                    x => x.IdPartida == ronda.IdPartida);

                if (!partida.Success)
                {
                    resultado.Success = false;
                    resultado.Error = partida.Error;
                    return resultado;
                }

                if (partida.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Partida no encontrada.";
                    return resultado;
                }

                // Un reintento del mismo nivel devuelve la ronda ya guardada
                var rondaExistente = await _unidadDeTrabajo.TRonda.ObtenerEntidadAsync(
                    x => x.IdPartida == ronda.IdPartida && x.IdNivel == ronda.IdNivel);

                if (!rondaExistente.Success)
                {
                    resultado.Success = false;
                    resultado.Error = rondaExistente.Error;
                    return resultado;
                }

                if (rondaExistente.Data != null)
                {
                    resultado.Data = _mapper.Map<TRonda>(rondaExistente.Data);
                    return resultado;
                }

                if (partida.Data.Estado != EstadoEnCurso)
                {
                    resultado.Success = false;
                    resultado.Error = "La partida ya terminó; no admite más niveles.";
                    return resultado;
                }

                var modo = ModosPartida.DesdeCodigo(partida.Data.Modo)!.Value;

                var errorValidacion =
                    ValidarRonda(ronda, partida.Data) ??
                    ValidarParticipantes(participantes, modo);

                if (errorValidacion != null)
                {
                    resultado.Success = false;
                    resultado.Error = errorValidacion;
                    return resultado;
                }

                errorValidacion = await ValidarReferenciasRondaAsync(ronda, participantes);

                if (errorValidacion != null)
                {
                    resultado.Success = false;
                    resultado.Error = errorValidacion;
                    return resultado;
                }

                var entidad = _mapper.Map<Ronda>(ronda);
                entidad.IdRonda = 0;

                _unidadDeTrabajo.EmpezarTransaccion();

                var respuestaRonda =
                    await _unidadDeTrabajo.TRonda.InsertarAsync(entidad);

                if (!respuestaRonda.Success || respuestaRonda.Data == null)
                {
                    _unidadDeTrabajo.Rollback();

                    resultado.Success = false;
                    resultado.Error = respuestaRonda.Error;
                    return resultado;
                }

                foreach (var participante in participantes)
                {
                    var entidadParticipante =
                        _mapper.Map<ParticipanteRonda>(participante);
                    entidadParticipante.IdParticipante = 0;
                    entidadParticipante.IdRonda = respuestaRonda.Data.IdRonda;

                    var respuestaParticipante =
                        await _unidadDeTrabajo.TParticipanteRonda.InsertarAsync(
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

                resultado.Data = _mapper.Map<TRonda>(respuestaRonda.Data);
            }
            catch (Exception ex)
            {
                _unidadDeTrabajo.Rollback();

                _logger.LogError(
                    ex,
                    "Error al guardar el nivel {IdNivel} de IdPartida {IdPartida}",
                    ronda?.IdNivel,
                    ronda?.IdPartida);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<TPartida>> FinalizarAsync(TPartida datos)
        {
            var resultado = new Respuesta<TPartida>();

            try
            {
                if (datos == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Faltan los datos de la partida.";
                    return resultado;
                }

                var estado = (datos.Estado ?? string.Empty).Trim().ToUpperInvariant();

                if (estado != EstadoFinalizada && estado != EstadoAbandonada)
                {
                    resultado.Success = false;
                    resultado.Error = "Estado no válido. Use FINALIZADA o ABANDONADA.";
                    return resultado;
                }

                if (datos.FechaFin == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Falta la fecha de fin de la partida.";
                    return resultado;
                }

                var partida = await _unidadDeTrabajo.TPartida.ObtenerEntidadAsync(
                    x => x.IdPartida == datos.IdPartida);

                if (!partida.Success)
                {
                    resultado.Success = false;
                    resultado.Error = partida.Error;
                    return resultado;
                }

                if (partida.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = "Partida no encontrada.";
                    return resultado;
                }

                // Un reintento del mismo cierre devuelve la partida ya cerrada
                if (partida.Data.Estado == estado)
                {
                    resultado.Data = _mapper.Map<TPartida>(partida.Data);
                    return resultado;
                }

                if (partida.Data.Estado != EstadoEnCurso)
                {
                    resultado.Success = false;
                    resultado.Error = $"La partida ya se cerró como {partida.Data.Estado}.";
                    return resultado;
                }

                if (datos.FechaFin < partida.Data.FechaInicio)
                {
                    resultado.Success = false;
                    resultado.Error = "La fecha de fin no puede ser anterior a la de inicio.";
                    return resultado;
                }

                if (estado == EstadoFinalizada)
                {
                    var rondas = await _unidadDeTrabajo.TRonda.ContarAsync(
                        x => x.IdPartida == datos.IdPartida);

                    if (!rondas.Success)
                    {
                        resultado.Success = false;
                        resultado.Error = rondas.Error;
                        return resultado;
                    }

                    if (rondas.Data == 0)
                    {
                        resultado.Success = false;
                        resultado.Error = "Una partida finalizada debe tener al menos un nivel jugado.";
                        return resultado;
                    }
                }

                partida.Data.Estado = estado;
                partida.Data.FechaFin = datos.FechaFin;

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TPartida.ModificarAsync(partida.Data);

                if (!respuestaRepositorio.Success || respuestaRepositorio.Data == null)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data = _mapper.Map<TPartida>(respuestaRepositorio.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al finalizar IdPartida {IdPartida}",
                    datos?.IdPartida);

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

        public async Task<Respuesta<IEnumerable<TRonda>>> ListarRondasAsync(int idPartida)
        {
            var resultado = new Respuesta<IEnumerable<TRonda>>();

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
                    await _unidadDeTrabajo.TRonda.BuscarAsync(
                        x => x.IdPartida == idPartida);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TRonda>>(respuestaRepositorio.Data)
                    .OrderBy(x => x.FechaInicio);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al listar rondas de IdPartida {IdPartida}",
                    idPartida);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        public async Task<Respuesta<IEnumerable<TParticipanteRonda>>> ListarParticipantesAsync(
            int idRonda)
        {
            var resultado = new Respuesta<IEnumerable<TParticipanteRonda>>();

            try
            {
                var rondaExistente =
                    await _unidadDeTrabajo.TRonda.ContarAsync(
                        x => x.IdRonda == idRonda);

                if (!rondaExistente.Success)
                {
                    resultado.Success = false;
                    resultado.Error = rondaExistente.Error;
                    return resultado;
                }

                if (rondaExistente.Data == 0)
                {
                    resultado.Success = false;
                    resultado.Error = "Ronda no encontrada.";
                    return resultado;
                }

                var respuestaRepositorio =
                    await _unidadDeTrabajo.TParticipanteRonda.BuscarAsync(
                        x => x.IdRonda == idRonda);

                if (!respuestaRepositorio.Success)
                {
                    resultado.Success = false;
                    resultado.Error = respuestaRepositorio.Error;
                    return resultado;
                }

                resultado.Data =
                    _mapper.Map<IEnumerable<TParticipanteRonda>>(
                        respuestaRepositorio.Data)
                    .OrderBy(x => x.IdPersonaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al listar participantes de IdRonda {IdRonda}",
                    idRonda);

                resultado.Success = false;
                resultado.Error = ex.GetBaseException().Message;
            }

            return resultado;
        }

        private static string? ValidarInicio(TPartida partida)
        {
            if (ModosPartida.DesdeCodigo(partida.Modo) == null)
                return "Modo no válido. Use MULTIJUGADOR o CONTRA_IA.";

            // También la partida contra la IA se inicia desde una sala
            if (partida.CodigoSala.Length == 0)
                return "Falta el código de sala.";

            if (partida.CodigoSala.Length > LargoMaximoCodigoSala)
                return "El código de sala admite como máximo seis caracteres.";

            // La fecha de inicio la pone la sala: sirve para detectar reintentos
            if (partida.FechaInicio == default)
                return "Falta la fecha de inicio de la partida.";

            return null;
        }

        private static string? ValidarRonda(TRonda ronda, Partida partida)
        {
            // null = ronda interrumpida, sin ganador
            if (ronda.BandoGanador != null &&
                CatalogoPersonajes.DesdeCodigo(ronda.BandoGanador) == null)
                return "Bando ganador no válido. Use PACMAN o FANTASMA.";

            if (ronda.FechaInicio == default)
                return "Falta la fecha de inicio del nivel.";

            if (ronda.FechaInicio < partida.FechaInicio)
                return "El nivel no puede empezar antes que la partida.";

            if (ronda.FechaFin < ronda.FechaInicio)
                return "La fecha de fin del nivel no puede ser anterior a la de inicio.";

            return null;
        }

        private static string? ValidarParticipantes(
            IReadOnlyCollection<TParticipanteRonda> participantes, ModoPartida modo)
        {
            if (participantes.Any(x => x == null))
                return "Hay un participante sin datos.";

            if (participantes.Select(x => x.IdPersonaje).Distinct().Count() !=
                participantes.Count)
                return "Un personaje no puede repetirse en el mismo nivel.";

            var pacmans = participantes.Count(x => CatalogoPersonajes.EsPacMan(x.IdPersonaje));
            var fantasmas = participantes.Count(x => CatalogoPersonajes.EsFantasma(x.IdPersonaje));

            if (pacmans + fantasmas != participantes.Count)
                return "Uno de los personajes no existe.";

            var pacmansEsperados = modo == ModoPartida.ContraIa
                ? ConfiguracionPartida.CantidadPacManContraIa
                : ConfiguracionPartida.CantidadPacMan;

            if (pacmans != pacmansEsperados ||
                fantasmas != ConfiguracionPartida.CantidadFantasmas)
                return $"El nivel debe tener {pacmansEsperados} Pac-Man y " +
                       $"{ConfiguracionPartida.CantidadFantasmas} fantasmas.";

            if (participantes.Any(x => x.EsIa != (x.IdUsuario == null)))
                return "La IA no tiene usuario y cada jugador humano debe tenerlo.";

            if (participantes.Any(x => x.EsIa && x.Abandono))
                return "Solo un jugador humano puede abandonar.";

            var humanos = participantes.Where(x => !x.EsIa).ToList();

            if (modo == ModoPartida.ContraIa && humanos.Count != 1)
                return "Contra la IA juega un solo humano.";

            if (humanos.Count == 0 || humanos.Count > ConfiguracionPartida.CantidadMaximaHumanos)
                return $"El nivel debe tener entre 1 y {ConfiguracionPartida.CantidadMaximaHumanos} jugadores humanos.";

            if (modo == ModoPartida.Multijugador &&
                humanos.Count(x => CatalogoPersonajes.EsFantasma(x.IdPersonaje)) >
                    ConfiguracionPartida.CantidadMaximaFantasmasHumanos)
                return "Como máximo 2 fantasmas pueden ser humanos.";

            if (humanos.Select(x => x.IdUsuario).Distinct().Count() != humanos.Count)
                return "Un usuario no puede aparecer dos veces en el mismo nivel.";

            if (participantes.Any(x =>
                x.Puntuacion < 0 ||
                x.FantasmasComidos < 0 ||
                x.PacmansAtrapados < 0 ||
                x.ObjetosUsados < 0))
                return "Las estadísticas de los participantes no pueden ser negativas.";

            return null;
        }

        private async Task<string?> ValidarReferenciasRondaAsync(
            TRonda ronda, IReadOnlyCollection<TParticipanteRonda> participantes)
        {
            var nivel = await _unidadDeTrabajo.TNivel.ContarAsync(
                x => x.IdNivel == ronda.IdNivel);

            if (!nivel.Success)
                return nivel.Error;

            if (nivel.Data == 0)
                return "Nivel no encontrado.";

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
    }
}
