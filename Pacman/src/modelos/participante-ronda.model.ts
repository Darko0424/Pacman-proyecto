export interface TParticipanteRonda {
    idParticipante: number;
    idRonda: number;
    idUsuario: number | null;
    idPersonaje: number;
    esIa: boolean;
    puntuacion: number;
    fantasmasComidos: number;
    pacmansAtrapados: number;
    objetosUsados: number;
    abandono: boolean;
}
