export interface TParticipantePartida {
    idParticipante: number;
    idPartida: number;
    idUsuario: number | null;
    idPersonaje: number;
    esIa: boolean;
    puntuacion: number;
    fantasmasComidos: number;
    pacmansAtrapados: number;
    frutasComidas: number;
    abandono: boolean;
}
