import { Bando, Direccion, TipoEventoJuego } from './enums';

interface ActorDto {
    idPersonaje: number;
    idUsuario: number | null;
    // null para la IA
    nombreUsuario: string | null;
    esIa: boolean;
    // Un humano desconectado o que abandonó: la IA mueve su personaje
    controlIa: boolean;
    fila: number;
    columna: number;
    direccion: Direccion;
    // Fracción recorrida hacia la siguiente casilla (0 a 1)
    avance: number;
    vidasRestantes: number;
    eliminado: boolean;
    reaparicionPendiente: boolean;
    esInvencible: boolean;
    idObjetoGuardado: number | null;
    puntuacion: number;
}

export type PacManDto = ActorDto;

export interface FantasmaDto extends ActorDto {
    asustado: boolean;
    aturdido: boolean;
}

export interface CajaDto {
    idCaja: number;
    fila: number;
    columna: number;
}

export interface EfectoDto {
    idEfecto: number;
    idObjeto: number;
    tipoEfecto: string;
    idPersonajeOrigen: number;
    bandoOrigen: Bando;
    finSegundos: number;
}

export interface EventoDto {
    tipo: TipoEventoJuego;
    // Quien provoca el evento
    idPersonaje: number | null;
    // El afectado, si lo hay
    idOtroPersonaje: number | null;
    fila: number | null;
    columna: number | null;
    idObjeto: number | null;
}

// Lo que cambia durante el nivel (EstadoActualizado)
export interface EstadoPartidaDto {
    codigoSala: string | null;
    numeroNivel: number;
    // Segundos desde el inicio del nivel; sirve para interpolar
    tiempoSimulacionSegundos: number;
    terminada: boolean;
    bandoGanador: Bando | null;
    bolasRestantes: number;
    // Índices fila * columnas + columna de las bolas ya comidas
    puntosConsumidos: number[];
    pacmans: PacManDto[];
    fantasmas: FantasmaDto[];
    // Solo las cajas que siguen en el mapa
    cajas: CajaDto[];
    efectos: EfectoDto[];
    // Lo que pasó desde el estado anterior (sonidos y animaciones)
    eventos: EventoDto[];
}
