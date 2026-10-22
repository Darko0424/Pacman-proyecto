import { CodigoRol } from './tiempo-real/enums';

export type RolRanking = Exclude<CodigoRol, null>;

export interface RankingEntrada {
    rol: RolRanking;
    idUsuario: number;
    nombreUsuario: string;
    mejorPuntuacion: number | null;
    nivelesEnRol: number;
    posicion: number | null;
}
