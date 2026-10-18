import { Scene } from 'phaser';
import {
    COLORES_FANTASMA,
    COLORES_PACMAN,
    COLUMNAS_FANTASMA,
    CUADROS_PACMAN,
    FILA_FANTASMA_ASUSTADO,
    FILA_FANTASMA_BLANCO,
    TEXTURA,
    aspectoFantasma,
    cuadroCuerpoFantasma,
    texturaPacMan,
} from '../sprites/CatalogoSprites';

export const ANIM_FANTASMA_ASUSTADO = 'fantasma-asustado';
// parpadea entre oscuro y blanco significa que el efecto ya casi terminar
export const ANIM_FANTASMA_ASUSTADO_FIN = 'fantasma-asustado-fin';

export function animPacManComer(color: string): string {
    return `${texturaPacMan(color)}-comer`;
}

export function animPacManMorir(color: string): string {
    return `${texturaPacMan(color)}-morir`;
}

export function animFantasmaMover(color: string): string {
    return `fantasma-${aspectoFantasma(color).fila}-mover`;
}

// Las animaciones son globales del juego: se registran una sola vez, en EscenaCarga
export function registrarAnimaciones(escena: Scene): void {
    const anims = escena.anims;

    for (const color of COLORES_PACMAN) {
        const textura = texturaPacMan(color);

        if (!anims.exists(animPacManComer(color))) {
            anims.create({
                key: animPacManComer(color),
                frames: anims.generateFrameNumbers(textura, { frames: CUADROS_PACMAN.COMER }),
                frameRate: 14,
                repeat: -1,
            });
        }

        if (!anims.exists(animPacManMorir(color))) {
            anims.create({
                key: animPacManMorir(color),
                frames: anims.generateFrameNumbers(textura, {
                    start: CUADROS_PACMAN.MORIR.inicio,
                    end: CUADROS_PACMAN.MORIR.fin,
                }),
                frameRate: 9,
                repeat: 0,
            });
        }
    }

    for (const color of COLORES_FANTASMA) {
        if (!anims.exists(animFantasmaMover(color))) {
            anims.create({
                key: animFantasmaMover(color),
                frames: anims.generateFrameNumbers(TEXTURA.FANTASMAS, {
                    frames: cuadrosDeFila(aspectoFantasma(color).fila),
                }),
                frameRate: 8,
                repeat: -1,
            });
        }
    }

    if (!anims.exists(ANIM_FANTASMA_ASUSTADO)) {
        anims.create({
            key: ANIM_FANTASMA_ASUSTADO,
            frames: anims.generateFrameNumbers(TEXTURA.FANTASMAS, {
                frames: cuadrosDeFila(FILA_FANTASMA_ASUSTADO),
            }),
            frameRate: 8,
            repeat: -1,
        });
    }

    if (!anims.exists(ANIM_FANTASMA_ASUSTADO_FIN)) {
        const oscuro = cuadrosDeFila(FILA_FANTASMA_ASUSTADO);
        const blanco = cuadrosDeFila(FILA_FANTASMA_BLANCO);

        anims.create({
            key: ANIM_FANTASMA_ASUSTADO_FIN,
            frames: anims.generateFrameNumbers(TEXTURA.FANTASMAS, {
                frames: [oscuro[0], oscuro[1], blanco[2], blanco[3]],
            }),
            frameRate: 8,
            repeat: -1,
        });
    }
}

function cuadrosDeFila(fila: number): number[] {
    return Array.from(
        { length: COLUMNAS_FANTASMA },
        (_, columna) => cuadroCuerpoFantasma(fila, columna));
}
