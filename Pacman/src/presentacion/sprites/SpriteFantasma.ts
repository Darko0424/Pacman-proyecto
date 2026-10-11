import { GameObjects, Scene } from 'phaser';
import {
    ANIM_FANTASMA_ASUSTADO,
    ANIM_FANTASMA_ASUSTADO_FIN,
    animFantasmaMover,
} from '../animaciones/Animaciones';
import {
    Direccion,
    TEXTURA,
    aspectoFantasma,
    cuadroBolaFantasma,
    cuadroCaraFantasma,
    cuadroCuerpoFantasma,
} from './CatalogoSprites';

export type ModoFantasma = 'NORMAL' | 'ASUSTADO' | 'ASUSTADO_FIN' | 'COMIDO';

// Cuánto se corre la cara hacia donde mira el fantasma
const DESPLAZAMIENTO_CARA = 2;

// Cuerpo animado + cara encima. Asustado usa cuadros que ya traen la cara;
// comido deja solo la cara.
export class SpriteFantasma extends GameObjects.Container {
    private readonly color: string;
    private readonly cuerpo: GameObjects.Sprite;
    private readonly cara: GameObjects.Image;
    private modo: ModoFantasma = 'NORMAL';
    private direccion: Direccion = 'IZQUIERDA';

    // color es personajes.color ("ROJO", "ROSA", "CELESTE")
    constructor(escena: Scene, x: number, y: number, color: string) {
        super(escena, x, y);

        this.color = color;
        this.cuerpo = escena.add.sprite(
            0, 0, TEXTURA.FANTASMAS, cuadroCuerpoFantasma(aspectoFantasma(color).fila));
        this.cara = escena.add.image(0, 0, TEXTURA.CARAS, cuadroCaraFantasma(color));

        this.add([this.cuerpo, this.cara]);
        escena.add.existing(this);

        this.aplicar();
    }

    mirar(direccion: Direccion): this {
        this.direccion = direccion;
        this.aplicar();

        return this;
    }

    establecerModo(modo: ModoFantasma): this {
        this.modo = modo;
        this.aplicar();

        return this;
    }

    private aplicar(): void {
        const comido = this.modo === 'COMIDO';
        const asustado = this.modo === 'ASUSTADO' || this.modo === 'ASUSTADO_FIN';

        this.cuerpo.setVisible(!comido);
        this.cara.setVisible(!asustado);
        this.cara.setFrame(
            comido ? cuadroBolaFantasma(this.color) : cuadroCaraFantasma(this.color));

        if (comido) {
            this.cuerpo.stop();
            this.cara.setPosition(0, 0);
            return;
        }

        this.cuerpo.play(
            this.modo === 'ASUSTADO' ? ANIM_FANTASMA_ASUSTADO
                : this.modo === 'ASUSTADO_FIN' ? ANIM_FANTASMA_ASUSTADO_FIN
                    : animFantasmaMover(this.color),
            true);

        this.cara.setPosition(
            this.direccion === 'IZQUIERDA' ? -DESPLAZAMIENTO_CARA
                : this.direccion === 'DERECHA' ? DESPLAZAMIENTO_CARA : 0,
            this.direccion === 'ARRIBA' ? -DESPLAZAMIENTO_CARA
                : this.direccion === 'ABAJO' ? DESPLAZAMIENTO_CARA : 0);
    }
}
