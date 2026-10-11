import { GameObjects, Scene } from 'phaser';
import { animPacManComer, animPacManMorir } from '../animaciones/Animaciones';
import { CUADROS_PACMAN, Direccion, texturaPacMan } from './CatalogoSprites';

export class SpritePacMan extends GameObjects.Sprite {
    private readonly color: string;

    // color es personajes.color ("AMARILLO" o "VERDE")
    constructor(escena: Scene, x: number, y: number, color: string) {
        super(escena, x, y, texturaPacMan(color), CUADROS_PACMAN.QUIETO);

        this.color = color;
        escena.add.existing(this);
    }

    // La hoja solo trae a Pac-Man mirando a la derecha: se gira o se voltea
    mirar(direccion: Direccion): this {
        this.setFlipX(direccion === 'IZQUIERDA');
        this.setAngle(direccion === 'ARRIBA' ? -90 : direccion === 'ABAJO' ? 90 : 0);

        return this;
    }

    comer(): this {
        this.play(animPacManComer(this.color), true);

        return this;
    }

    detener(): this {
        this.stop();
        this.setFrame(CUADROS_PACMAN.QUIETO);

        return this;
    }

    morir(): this {
        this.setFlipX(false);
        this.setAngle(0);
        this.play(animPacManMorir(this.color));

        return this;
    }
}
