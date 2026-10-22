import { RolRanking } from './ranking-entrada.model';

export interface HistorialRonda {
    idUsuario: number | null;
    idPartida: number;
    idRonda: number;
    numeroNivel: number;
    nivel: string;
    personaje: string;
    rol: RolRanking;
    puntuacion: number;
    fantasmasComidos: number;
    pacmansAtrapados: number;
    objetosUsados: number;
    abandono: boolean;
    bandoGanador: RolRanking | null;
    gano: number | null;
    fechaInicio: string;
    fechaFin: string;
}
