import { GameObjects, Scene } from 'phaser';
import { SONIDO } from '../sonidos/CatalogoSonidos';
import { COLOR, css, estiloTexto } from './Paleta';
import { Recuadro } from './Panel';

export interface OpcionesBoton {
    ancho?: number;
    alto?: number;
    // Color del borde y del relleno al pasar el puntero
    color?: number;
    tamanoTexto?: number;
    alPulsar: () => void;
}

// Botón con el trazo de los muros: al pasar el puntero se rellena con su color
export class Boton extends GameObjects.Container {
    private readonly fondo: Recuadro;
    private readonly zona: GameObjects.Zone;
    private readonly etiqueta: GameObjects.Text;
    private readonly color: number;
    private habilitado = true;

    constructor(escena: Scene, x: number, y: number, texto: string, opciones: OpcionesBoton) {
        super(escena, x, y);

        const ancho = opciones.ancho ?? 240;
        const alto = opciones.alto ?? 56;
        this.color = opciones.color ?? COLOR.MURO_CLARO;

        this.fondo = new Recuadro(escena, 0, 0, ancho, alto, 10, true).dibujar(COLOR.PANEL, this.color);
        this.zona = escena.add.zone(0, 0, ancho, alto).setInteractive({ useHandCursor: true });

        this.etiqueta = escena.add
            .text(0, 0, texto, estiloTexto(opciones.tamanoTexto ?? 14, this.color))
            .setOrigin(0.5);

        this.add([this.fondo, this.etiqueta, this.zona]);
        escena.add.existing(this);

        this.zona.on('pointerover', () => this.resaltar(true));
        this.zona.on('pointerout', () => this.resaltar(false));
        this.zona.on('pointerdown', () => this.setScale(0.96));
        this.zona.on('pointerup', () => {
            this.setScale(1);

            if (this.habilitado) {
                escena.sound.play(SONIDO.COMER_FRUTA, { volume: 0.4 });
                opciones.alPulsar();
            }
        });
    }

    setTexto(texto: string): this {
        this.etiqueta.setText(texto);

        return this;
    }

    setHabilitado(habilitado: boolean): this {
        this.habilitado = habilitado;
        this.setAlpha(habilitado ? 1 : 0.35);

        if (habilitado) {
            this.zona.setInteractive({ useHandCursor: true });
        } else {
            this.zona.disableInteractive();
            this.resaltar(false);
        }

        return this;
    }

    private resaltar(activo: boolean): void {
        this.fondo.dibujar(activo ? this.color : COLOR.PANEL, this.color);
        this.etiqueta.setColor(css(activo ? COLOR.FONDO : this.color));
    }
}
