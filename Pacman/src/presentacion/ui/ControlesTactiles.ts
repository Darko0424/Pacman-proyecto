import { GameObjects, Geom, Input, Scene } from 'phaser';
import { Direccion } from '../../modelos/tiempo-real/enums';
import { COLOR, estiloTexto } from './Paleta';
import { Recuadro } from './Panel';

// Distancia que hay que deslizar el dedo para que cuente como dirección
const UMBRAL_DESLIZAR = 24;
const LADO_BOTON = 64;

// Controles para celular: deslizar el dedo sobre el mapa y una cruceta.
// Solo avisan la dirección elegida; quien los usa decide si la envía.
export class ControlesTactiles {
    private inicio: { x: number; y: number } | null = null;
    private ladoBoton = LADO_BOTON;

    constructor(
        private readonly escena: Scene,
        private readonly zonaDeslizar: Geom.Rectangle,
        private readonly alElegir: (direccion: Direccion) => void,
    ) {
        escena.input.on('pointerdown', this.alPresionar, this);
        escena.input.on('pointermove', this.alMover, this);
        escena.input.on('pointerup', this.alSoltar, this);

        escena.events.once('shutdown', () => {
            escena.input.off('pointerdown', this.alPresionar, this);
            escena.input.off('pointermove', this.alMover, this);
            escena.input.off('pointerup', this.alSoltar, this);
        });
    }

    // Cruceta de cuatro botones centrada en (x, y)
    crearCruceta(x: number, y: number, lado = LADO_BOTON): GameObjects.Container {
        this.ladoBoton = lado;
        const cruceta = this.escena.add.container(x, y);
        const paso = lado + 6;

        cruceta.add([
            this.crearBoton(0, -paso, '^', Direccion.Arriba),
            this.crearBoton(0, paso, 'v', Direccion.Abajo),
            this.crearBoton(-paso, 0, '<', Direccion.Izquierda),
            this.crearBoton(paso, 0, '>', Direccion.Derecha),
        ]);

        return cruceta;
    }

    private crearBoton(x: number, y: number, texto: string, direccion: Direccion): GameObjects.Container {
        const boton = this.escena.add.container(x, y);
        const fondo = new Recuadro(this.escena, 0, 0, this.ladoBoton, this.ladoBoton, 10, true)
            .dibujar(COLOR.PANEL, COLOR.MURO_CLARO);
        const zona = this.escena.add.zone(0, 0, this.ladoBoton, this.ladoBoton).setInteractive();

        // Se reacciona al tocar, no al soltar: en el juego cada instante cuenta
        zona.on('pointerdown', () => {
            fondo.dibujar(COLOR.MURO_CLARO, COLOR.MURO_CLARO);
            this.alElegir(direccion);
        });
        zona.on('pointerup', () => fondo.dibujar(COLOR.PANEL, COLOR.MURO_CLARO));
        zona.on('pointerout', () => fondo.dibujar(COLOR.PANEL, COLOR.MURO_CLARO));

        boton.add([fondo, this.escena.add.text(0, 0, texto, estiloTexto(20, COLOR.MURO_CLARO)).setOrigin(0.5), zona]);

        return boton;
    }

    private alPresionar(puntero: Input.Pointer): void {
        this.inicio = this.zonaDeslizar.contains(puntero.x, puntero.y) ? { x: puntero.x, y: puntero.y } : null;
    }

    // Cada vez que el dedo recorre el umbral cuenta como un giro, sin tener que soltar
    private alMover(puntero: Input.Pointer): void {
        if (!this.inicio || !puntero.isDown) {
            return;
        }

        const dx = puntero.x - this.inicio.x;
        const dy = puntero.y - this.inicio.y;

        if (Math.max(Math.abs(dx), Math.abs(dy)) < UMBRAL_DESLIZAR) {
            return;
        }

        if (Math.abs(dx) > Math.abs(dy)) {
            this.alElegir(dx > 0 ? Direccion.Derecha : Direccion.Izquierda);
        } else {
            this.alElegir(dy > 0 ? Direccion.Abajo : Direccion.Arriba);
        }

        this.inicio = { x: puntero.x, y: puntero.y };
    }

    private alSoltar(): void {
        this.inicio = null;
    }
}
