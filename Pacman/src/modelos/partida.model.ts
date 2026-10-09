export interface TPartida {
    idPartida: number;
    codigoSala: string | null;
    modo: string;
    idDificultad: number;
    idNivelAlcanzado: number;
    estado: string;
    bandoGanador: string | null;
    fechaInicio?: string | Date;
    fechaFin?: string | Date | null;
}
