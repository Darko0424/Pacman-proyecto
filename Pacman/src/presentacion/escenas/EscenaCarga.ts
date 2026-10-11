import { Scene } from 'phaser';
import { registrarAnimaciones } from '../animaciones/Animaciones';
import {
    TAMANO_ACTOR,
    TAMANO_CASILLA,
    TAMANO_ITEM,
    TEXTURA,
} from '../sprites/CatalogoSprites';

// Carga todos los sprites, registra las animaciones y pasa al menú
export class EscenaCarga extends Scene {
    constructor() {
        super('EscenaCarga');
    }

    preload(): void {
        const { width, height } = this.scale;

        this.add.rectangle(width / 2, height / 2, 468, 32).setStrokeStyle(1, 0xffffff);
        const barra = this.add.rectangle(width / 2 - 230, height / 2, 4, 28, 0xffffff);

        this.load.on('progress', (progreso: number) => {
            barra.width = 4 + 460 * progreso;
        });

        this.load.setPath('assets');

        // Todavía las usan las escenas de la plantilla (MainMenu, Game, GameOver)
        this.load.image('background', 'bg.png');
        this.load.image('logo', 'logo.png');

        const actor = { frameWidth: TAMANO_ACTOR, frameHeight: TAMANO_ACTOR };
        const item = { frameWidth: TAMANO_ITEM, frameHeight: TAMANO_ITEM };
        const casilla = { frameWidth: TAMANO_CASILLA, frameHeight: TAMANO_CASILLA };

        this.load.spritesheet(TEXTURA.PACMAN_AMARILLO, 'sprites/pacman-amarillo.png', actor);
        this.load.spritesheet(TEXTURA.PACMAN_VERDE, 'sprites/pacman-verde.png', actor);
        this.load.spritesheet(TEXTURA.FANTASMAS, 'sprites/fantasmas.png', actor);
        this.load.spritesheet(TEXTURA.CARAS, 'sprites/fantasmas.png', item);
        this.load.spritesheet(TEXTURA.ITEMS, 'sprites/items.png', item);
        this.load.spritesheet(TEXTURA.TILES, 'sprites/tiles.png', casilla);
        this.load.image(TEXTURA.LABERINTO_CLASICO, 'sprites/laberinto-clasico.png');
    }

    create(): void {
        registrarAnimaciones(this);

        // http://localhost:8080/?sprites abre la galería para revisar los sprites
        const verGaleria = new URLSearchParams(window.location.search).has('sprites');

        this.scene.start(verGaleria ? 'EscenaGaleriaSprites' : 'MainMenu');
    }
}
