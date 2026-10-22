import { TPartida } from './partida.model';

export interface HistorialEntrada {
    idUsuario: number | null;
    idPartida: number;
    codigoSala: string;
    modo: TPartida['modo'];
    dificultad: string;
    estado: TPartida['estado'];
    fechaInicio: string;
    fechaFin: string | null;
    nivelesJugados: number;
    nivelAlcanzado: number;
    puntuacion: number;
    fantasmasComidos: number;
    pacmansAtrapados: number;
    objetosUsados: number;
    nivelesGanados: number;
    abandono: boolean;
}
