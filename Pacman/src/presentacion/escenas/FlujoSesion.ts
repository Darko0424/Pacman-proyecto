import { GameObjects, Scene } from 'phaser';
import { TUsuario } from '../../modelos/usuario.model';
import { COLOR, css, estiloTexto } from '../ui/Paleta';
import { dibujarMarco } from '../ui/Panel';


export const ESCENA_PRINCIPAL = 'EscenaMenu';


export function continuarConSesion(escena: Scene, usuario: TUsuario): void {
    escena.scene.start(usuario.nombreDefinido ? ESCENA_PRINCIPAL : 'EscenaNombre');
}

export function dibujarPantallaSesion(escena: Scene, subtitulo: string): void {
    const centro = escena.scale.width / 2;

    dibujarMarco(escena);
    escena.add.text(centro, 100, 'PAC-MAN', estiloTexto(48, COLOR.PACMAN)).setOrigin(0.5);
    escena.add.text(centro, 160, subtitulo, estiloTexto(16, COLOR.GRIS_CLARO)).setOrigin(0.5);
}

export class Aviso {
    private readonly texto: GameObjects.Text;

    constructor(escena: Scene, y: number) {
        this.texto = escena.add
            .text(escena.scale.width / 2, y, '', estiloTexto(11, COLOR.GRIS_CLARO))
            .setOrigin(0.5)
            .setAlign('center')
            .setWordWrapWidth(760);
    }

    error(texto: string): void {
        this.texto.setColor(css(COLOR.ROSA)).setText(texto);
    }

    info(texto: string): void {
        this.texto.setColor(css(COLOR.GRIS_CLARO)).setText(texto);
    }

    exito(texto: string): void {
        this.texto.setColor(css(COLOR.VERDE)).setText(texto);
    }

    limpiar(): void {
        this.texto.setText('');
    }
}