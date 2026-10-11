import { Scene } from 'phaser';
import { animPacManMorir } from '../animaciones/Animaciones';
import {
    COLORES_FANTASMA,
    COLORES_PACMAN,
    CUADRO_ITEM,
    Direccion,
    TEXTURA,
} from '../sprites/CatalogoSprites';
import { ModoFantasma, SpriteFantasma } from '../sprites/SpriteFantasma';
import { SpritePacMan } from '../sprites/SpritePacMan';

const DIRECCIONES: Direccion[] = ['DERECHA', 'ABAJO', 'IZQUIERDA', 'ARRIBA'];
const MODOS_ESPECIALES: ModoFantasma[] = ['ASUSTADO', 'ASUSTADO_FIN', 'COMIDO'];

const COLUMNA = 530;
const PASO = 90;
const ESCALA = 2;

// Solo para desarrollo: muestra todos los sprites y animaciones cargados.
// Se abre con http://localhost:8080/?sprites
export class EscenaGaleriaSprites extends Scene {
    constructor() {
        super('EscenaGaleriaSprites');
    }

    create(): void {
        this.cameras.main.setBackgroundColor(0x1b1b26);

        this.dibujarLaberinto(20, 64);

        this.titulo(COLUMNA, 24, 'Galería de sprites');

        let fila = 110;

        for (const color of COLORES_PACMAN) {
            this.titulo(COLUMNA, fila - 52, `Pac-Man (${color})`);

            DIRECCIONES.forEach((direccion, i) => {
                const x = COLUMNA + 40 + i * PASO;

                new SpritePacMan(this, x, fila, color).setScale(ESCALA).mirar(direccion).comer();
                this.nota(x, fila + 38, direccion);
            });

            const x = COLUMNA + 40 + DIRECCIONES.length * PASO;

            new SpritePacMan(this, x, fila, color)
                .setScale(ESCALA)
                .play({ key: animPacManMorir(color), repeat: -1, repeatDelay: 700 });
            this.nota(x, fila + 38, 'MORIR');

            fila += 115;
        }

        this.titulo(COLUMNA, fila - 52, 'Fantasmas');

        COLORES_FANTASMA.forEach((color, i) => {
            const x = COLUMNA + 40 + i * PASO;

            new SpriteFantasma(this, x, fila, color)
                .setScale(ESCALA)
                .mirar(DIRECCIONES[i % DIRECCIONES.length]);
            this.nota(x, fila + 38, color);
        });

        fila += 115;
        this.titulo(COLUMNA, fila - 52, 'Fantasma: estados');

        MODOS_ESPECIALES.forEach((modo, i) => {
            const x = COLUMNA + 40 + i * (PASO + 30);

            new SpriteFantasma(this, x, fila, 'ROJO').setScale(ESCALA).establecerModo(modo);
            this.nota(x, fila + 38, modo);
        });

        fila += 115;
        this.titulo(COLUMNA, fila - 52, 'Frutas y puntos');

        Object.values(CUADRO_ITEM).forEach((cuadro, i) => {
            this.add.image(COLUMNA + 24 + i * 46, fila - 8, TEXTURA.ITEMS, cuadro).setScale(ESCALA);
        });
    }

    // Laberinto clásico del pack con los personajes encima, a tamaño real
    private dibujarLaberinto(x: number, y: number): void {
        this.add.image(x, y, TEXTURA.LABERINTO_CLASICO).setOrigin(0);

        new SpritePacMan(this, x + 240, y + 472, 'AMARILLO').mirar('IZQUIERDA').comer();
        new SpritePacMan(this, x + 240, y + 232, 'VERDE').mirar('DERECHA').comer();

        new SpriteFantasma(this, x + 240, y + 280, 'ROJO').mirar('IZQUIERDA');
        new SpriteFantasma(this, x + 208, y + 328, 'CELESTE').mirar('ARRIBA');
        new SpriteFantasma(this, x + 240, y + 328, 'ROSA').mirar('ABAJO');
        new SpriteFantasma(this, x + 272, y + 328, 'NARANJA').establecerModo('ASUSTADO');

        this.add.image(x + 240, y + 376, TEXTURA.ITEMS, CUADRO_ITEM.CEREZA);
        this.add.image(x + 40, y + 168, TEXTURA.ITEMS, CUADRO_ITEM.PUNTO_PODER);

        for (let i = 0; i < 6; i++) {
            this.add.image(x + 56 + i * 16, y + 136, TEXTURA.ITEMS, CUADRO_ITEM.PUNTO);
        }
    }

    private titulo(x: number, y: number, texto: string): void {
        this.add.text(x, y, texto, { fontFamily: 'Arial', fontSize: 16, color: '#ffffff' });
    }

    private nota(x: number, y: number, texto: string): void {
        this.add
            .text(x, y, texto, { fontFamily: 'Arial', fontSize: 11, color: '#9aa0b4' })
            .setOrigin(0.5, 0);
    }
}
