import { GameObjects, Scene } from 'phaser';
import { COLOR } from './Paleta';

// Rectángulo de esquinas redondeadas que se puede volver a pintar.
// Se dibuja con Graphics porque en Phaser 4.0.0 Rectangle.setRounded con
// relleno rompe el renderizado WebGL ("pipeline is not defined")
export class Recuadro extends GameObjects.Graphics {
    constructor(
        escena: Scene,
        x: number,
        y: number,
        private readonly ancho: number,
        private readonly alto: number,
        private readonly radio = 10,
        // true: (x, y) es el centro; false: la esquina superior izquierda
        private readonly centrado = false,
    ) {
        super(escena, { x, y });
        escena.add.existing(this);
    }

    // null deja sin relleno o sin borde
    dibujar(relleno: number | null, borde: number | null = null, grosor = 3): this {
        const x = this.centrado ? -this.ancho / 2 : 0;
        const y = this.centrado ? -this.alto / 2 : 0;

        this.clear();

        if (relleno !== null) {
            this.fillStyle(relleno).fillRoundedRect(x, y, this.ancho, this.alto, this.radio);
        }

        if (borde !== null) {
            this.lineStyle(grosor, borde).strokeRoundedRect(x, y, this.ancho, this.alto, this.radio);
        }

        return this;
    }
}

// Recuadro con el mismo trazo que los muros del laberinto con borde azul fondo oscuro
export function crearPanel(
    escena: Scene,
    x: number,
    y: number,
    ancho: number,
    alto: number,
    borde: number = COLOR.MURO,
): Recuadro {
    return new Recuadro(escena, x, y, ancho, alto).dibujar(COLOR.PANEL, borde);
}

// Marco morado de toda la pantalla como el del laberinto
export function dibujarMarco(escena: Scene): void {
    const { width, height } = escena.scale;

    escena.cameras.main.setBackgroundColor(COLOR.FONDO);
    escena.add.rectangle(width / 2, height / 2, width - 16, height - 16).setStrokeStyle(16, COLOR.MARCO);
}
