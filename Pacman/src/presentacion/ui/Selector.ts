import { GameObjects, Scene } from 'phaser';
import { SONIDO } from '../sonidos/CatalogoSonidos';
import { COLOR, css, estiloTexto } from './Paleta';

export interface OpcionSelector<T> {
    valor: T;
    texto: string;
}

// Fila "Etiqueta   < VALOR >" para el menú de configuración. Las flechas dan la
// vuelta al llegar al final. Si no es editable solo muestra el valor.
export class Selector<T> extends GameObjects.Container {
    private readonly valorTexto: GameObjects.Text;
    private readonly flechas: GameObjects.Text[];
    private indice = 0;

    constructor(
        escena: Scene,
        x: number,
        y: number,
        ancho: number,
        etiqueta: string,
        private readonly opciones: OpcionSelector<T>[],
        private readonly alCambiar: (valor: T) => void,
    ) {
        super(escena, x, y);

        const etiquetaTexto = escena.add.text(0, 0, etiqueta, estiloTexto(12, COLOR.GRIS_CLARO)).setOrigin(0, 0.5);

        // El valor ocupa la mitad derecha, centrado entre las flechas
        const centroValor = ancho - 110;
        this.valorTexto = escena.add.text(centroValor, 0, '', estiloTexto(12, COLOR.PACMAN)).setOrigin(0.5);

        const izquierda = this.crearFlecha(centroValor - 100, '<', -1);
        const derecha = this.crearFlecha(centroValor + 100, '>', 1);
        this.flechas = [izquierda, derecha];

        this.add([etiquetaTexto, this.valorTexto, izquierda, derecha]);
        escena.add.existing(this);
    }

    setValor(valor: T): this {
        const indice = this.opciones.findIndex((opcion) => opcion.valor === valor);

        this.indice = Math.max(indice, 0);
        this.valorTexto.setText(this.opciones[this.indice]?.texto ?? '-');

        return this;
    }

    setEditable(editable: boolean): this {
        for (const flecha of this.flechas) {
            flecha.setVisible(editable);
        }

        return this;
    }

    private crearFlecha(x: number, texto: string, paso: number): GameObjects.Text {
        const flecha = this.scene.add
            .text(x, 0, texto, estiloTexto(16, COLOR.MURO_CLARO))
            .setOrigin(0.5)
            .setPadding(12, 8, 12, 8)
            .setInteractive({ useHandCursor: true });

        flecha.on('pointerover', () => flecha.setColor(css(COLOR.BLANCO)));
        flecha.on('pointerout', () => flecha.setColor(css(COLOR.MURO_CLARO)));
        flecha.on('pointerup', () => {
            const total = this.opciones.length;
            this.indice = (this.indice + paso + total) % total;
            this.valorTexto.setText(this.opciones[this.indice].texto);
            this.scene.sound.play(SONIDO.WAKA, { volume: 0.3 });
            this.alCambiar(this.opciones[this.indice].valor);
        });

        return flecha;
    }
}
