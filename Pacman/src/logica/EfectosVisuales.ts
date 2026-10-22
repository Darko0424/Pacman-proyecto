import { Bando } from '../modelos/tiempo-real/enums';
import { EfectoDto } from '../modelos/tiempo-real/estado-partida.dto';
import { ActorDto } from './EstadoCliente';

// Claves de objetos.efecto (06_DatosCatalogos.sql), las mismas del motor
export const EFECTO = {
    VELOCIDAD: 'VELOCIDAD',
    ESCUDO: 'ESCUDO',
    CONGELAR: 'CONGELAR',
    PUNTOS_DOBLES: 'PUNTOS_DOBLES',
    RALENTIZAR: 'RALENTIZAR',
    TELETRANSPORTE: 'TELETRANSPORTE',
    TRAMPA: 'TRAMPA',
} as const;

// Qué hace cada objeto, en pocas palabras para el HUD
export const DESCRIPCION_EFECTO: Record<string, string> = {
    [EFECTO.VELOCIDAD]: 'MAS VELOCIDAD',
    [EFECTO.ESCUDO]: 'NO TE ATRAPAN',
    [EFECTO.CONGELAR]: 'CONGELA A LOS FANTASMAS',
    [EFECTO.PUNTOS_DOBLES]: 'PUNTOS X2',
    [EFECTO.RALENTIZAR]: 'FRENA A LOS PAC-MAN',
    [EFECTO.TELETRANSPORTE]: 'SALTO HACIA ADELANTE',
    [EFECTO.TRAMPA]: 'DEJA UNA TRAMPA DE HIELO',
};

// Cómo se ve un personaje según los efectos que lo afectan
export interface EstadoVisual {
    congelado: boolean;
    ralentizado: boolean;
    acelerado: boolean;
    escudo: boolean;
    puntosDobles: boolean;
}

// El servidor manda los efectos activos; aquí se decide a quién afecta cada uno
// (igual que las clases Efecto* del motor). Los que afectan al bando rival
// son los que vienen de otro bando.
export function estadoVisual(actor: ActorDto, bando: Bando, efectos: EfectoDto[]): EstadoVisual {
    const propio = (tipo: string) =>
        efectos.some((efecto) => efecto.tipoEfecto === tipo && efecto.idPersonajeOrigen === actor.idPersonaje);
    const delRival = (tipo: string) =>
        efectos.some((efecto) => efecto.tipoEfecto === tipo && efecto.bandoOrigen !== bando);

    return {
        congelado: actor.congelado || delRival(EFECTO.CONGELAR),
        ralentizado: delRival(EFECTO.RALENTIZAR),
        acelerado: propio(EFECTO.VELOCIDAD),
        escudo: propio(EFECTO.ESCUDO),
        puntosDobles: propio(EFECTO.PUNTOS_DOBLES),
    };
}
