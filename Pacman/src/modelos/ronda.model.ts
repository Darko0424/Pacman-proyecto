// Un nivel jugado dentro de una partida
export interface TRonda {
    idRonda: number;
    idPartida: number;
    idNivel: number;
    bandoGanador: 'PACMAN' | 'FANTASMA' | null;
    fechaInicio?: string | Date;
    fechaFin?: string | Date;
}
