// Una partida es la serie de niveles jugada en una sala
export interface TPartida {
    idPartida: number;
    codigoSala: string;
    modo: 'MULTIJUGADOR' | 'CONTRA_IA';
    idDificultad: number;
    estado: 'EN_CURSO' | 'FINALIZADA' | 'ABANDONADA';
    fechaInicio?: string | Date;
    fechaFin?: string | Date | null;
}
