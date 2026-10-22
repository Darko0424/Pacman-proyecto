import { GameObjects, Scene } from 'phaser';
import { CajaDto, TrampaDto } from '../../modelos/tiempo-real/estado-partida.dto';
import { NivelDto } from '../../modelos/tiempo-real/nivel.dto';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { Recuadro } from '../ui/Panel';
import { CUADRO_ITEM, TEXTURA } from './CatalogoSprites';

// Lo que se adelgaza un muro hacia el pasillo, en fracción de casilla
const MARGEN_MURO = 0.25;
const GROSOR_BORDE = 2;

// Los Pac-Man apenas ven las trampas; los fantasmas, claras
const ALFA_TRAMPA_OCULTA = 0.3;

// Tamaño de casilla al que los sprites (32 px) se ven a escala 1
export const CASILLA_BASE = 24;

// Mapa del nivel: muros, bolas y cajas. Las coordenadas que recibe son casillas
// (fila, columna), con decimales cuando un personaje va entre dos.
export class VistaLaberinto {
    private readonly capa: GameObjects.Container;
    private readonly puntos = new Map<number, GameObjects.Image>();
    private readonly cajas = new Map<number, GameObjects.Container>();
    private readonly trampas = new Map<number, GameObjects.Container>();
    private cantidadConsumidos = -1;

    constructor(
        private readonly escena: Scene,
        private readonly nivel: NivelDto,
        private readonly x: number,
        private readonly y: number,
        readonly tamano: number,
    ) {
        this.capa = escena.add.container(0, 0);

        this.dibujarMuros();
        this.dibujarPuntos();
    }

    get ancho(): number {
        return this.nivel.columnas * this.tamano;
    }

    get alto(): number {
        return this.nivel.filas * this.tamano;
    }

    // Centro en pantalla de una casilla
    centro(fila: number, columna: number): { x: number; y: number } {
        return {
            x: this.x + (columna + 0.5) * this.tamano,
            y: this.y + (fila + 0.5) * this.tamano,
        };
    }

    actualizarPuntos(consumidos: number[]): void {
        // Las bolas solo desaparecen: si no cambió la cantidad no hay nada que hacer
        if (consumidos.length === this.cantidadConsumidos) {
            return;
        }

        this.cantidadConsumidos = consumidos.length;
        const comidos = new Set(consumidos);

        for (const [indice, punto] of this.puntos) {
            punto.setVisible(!comidos.has(indice));
        }
    }

    actualizarCajas(cajas: CajaDto[]): void {
        const vigentes = new Set(cajas.map((caja) => caja.idCaja));

        for (const [id, vista] of this.cajas) {
            if (!vigentes.has(id)) {
                vista.destroy();
                this.cajas.delete(id);
            }
        }

        for (const caja of cajas) {
            if (!this.cajas.has(caja.idCaja)) {
                this.cajas.set(caja.idCaja, this.crearCaja(caja.fila, caja.columna));
            }
        }
    }

    // visibles: true para los fantasmas (sus trampas), false para los Pac-Man
    actualizarTrampas(trampas: TrampaDto[], visibles: boolean): void {
        const vigentes = new Set(trampas.map((trampa) => trampa.idTrampa));

        for (const [id, vista] of this.trampas) {
            if (!vigentes.has(id)) {
                vista.destroy();
                this.trampas.delete(id);
            }
        }

        for (const trampa of trampas) {
            let vista = this.trampas.get(trampa.idTrampa);

            if (!vista) {
                vista = this.crearTrampa(trampa.fila, trampa.columna);
                this.trampas.set(trampa.idTrampa, vista);
            }

            vista.setAlpha(visibles ? 1 : ALFA_TRAMPA_OCULTA);
        }
    }

    destruir(): void {
        this.capa.destroy();
        this.cajas.forEach((caja) => caja.destroy());
        this.cajas.clear();
        this.trampas.forEach((trampa) => trampa.destroy());
        this.trampas.clear();
        this.puntos.clear();
    }

    // ---------- Muros ----------

    // Mismo estilo que el laberinto clásico de los sprites: bloques azules con
    // borde celeste y esquinas redondeadas. Se pinta dos veces la misma forma:
    // primero un poco más gruesa en celeste (el borde) y encima la azul.
    private dibujarMuros(): void {
        const grafico = this.escena.add.graphics();
        const margen = this.tamano * MARGEN_MURO;

        this.pintarForma(grafico, margen - GROSOR_BORDE, COLOR.MURO_CLARO);
        this.pintarForma(grafico, margen, COLOR.MURO);

        this.capa.add(grafico);
    }

    private esMuro(fila: number, columna: number): boolean {
        // Fuera del mapa cuenta como pasillo: así el contorno exterior también lleva borde
        return this.nivel.casillas[fila]?.[columna] === '#';
    }

    // Cada muro es un cuadrado central más un brazo hacia cada muro vecino.
    // Las esquinas solo se rellenan si los tres vecinos de ese lado son muro,
    // así los rincones hacia el pasillo quedan recortados.
    private pintarForma(grafico: GameObjects.Graphics, hueco: number, color: number): void {
        const t = this.tamano;
        const radio = Math.max(2, hueco);

        grafico.fillStyle(color);

        for (let fila = 0; fila < this.nivel.filas; fila++) {
            for (let columna = 0; columna < this.nivel.columnas; columna++) {
                if (!this.esMuro(fila, columna)) {
                    continue;
                }

                const x = this.x + columna * t;
                const y = this.y + fila * t;
                const arriba = this.esMuro(fila - 1, columna);
                const abajo = this.esMuro(fila + 1, columna);
                const izquierda = this.esMuro(fila, columna - 1);
                const derecha = this.esMuro(fila, columna + 1);

                grafico.fillRoundedRect(x + hueco, y + hueco, t - 2 * hueco, t - 2 * hueco, {
                    tl: !arriba && !izquierda ? radio : 0,
                    tr: !arriba && !derecha ? radio : 0,
                    bl: !abajo && !izquierda ? radio : 0,
                    br: !abajo && !derecha ? radio : 0,
                });

                if (arriba) grafico.fillRect(x + hueco, y, t - 2 * hueco, hueco);
                if (abajo) grafico.fillRect(x + hueco, y + t - hueco, t - 2 * hueco, hueco);
                if (izquierda) grafico.fillRect(x, y + hueco, hueco, t - 2 * hueco);
                if (derecha) grafico.fillRect(x + t - hueco, y + hueco, hueco, t - 2 * hueco);

                if (arriba && izquierda && this.esMuro(fila - 1, columna - 1)) grafico.fillRect(x, y, hueco, hueco);
                if (arriba && derecha && this.esMuro(fila - 1, columna + 1)) grafico.fillRect(x + t - hueco, y, hueco, hueco);
                if (abajo && izquierda && this.esMuro(fila + 1, columna - 1)) grafico.fillRect(x, y + t - hueco, hueco, hueco);
                if (abajo && derecha && this.esMuro(fila + 1, columna + 1)) grafico.fillRect(x + t - hueco, y + t - hueco, hueco, hueco);
            }
        }
    }

    // ---------- Bolas y cajas ----------

    private dibujarPuntos(): void {
        const escala = this.tamano / CASILLA_BASE;

        for (const punto of this.nivel.puntos) {
            const { x, y } = this.centro(punto.fila, punto.columna);
            const imagen = this.escena.add
                .image(x, y, TEXTURA.ITEMS, punto.esDePoder ? CUADRO_ITEM.PUNTO_PODER : CUADRO_ITEM.PUNTO)
                .setScale(escala);

            if (punto.esDePoder) {
                this.escena.tweens.add({ targets: imagen, alpha: 0.2, duration: 300, yoyo: true, repeat: -1 });
            }

            this.puntos.set(punto.fila * this.nivel.columnas + punto.columna, imagen);
            this.capa.add(imagen);
        }
    }

    // Trampa de hielo: rombo celeste sobre el piso, debajo de los personajes
    private crearTrampa(fila: number, columna: number): GameObjects.Container {
        const { x, y } = this.centro(fila, columna);
        const lado = this.tamano * 0.6;
        const trampa = this.escena.add.container(x, y).setDepth(1);
        const rombo = new Recuadro(this.escena, 0, 0, lado, lado, 3, true).dibujar(COLOR.HIELO, COLOR.BLANCO, 2);

        rombo.setAngle(45);
        trampa.add(rombo);
        this.escena.tweens.add({ targets: rombo, scale: 0.8, duration: 600, yoyo: true, repeat: -1 });

        return trampa;
    }

    // Caja de objeto estilo Mario Kart: cuadro con "?" que flota
    private crearCaja(fila: number, columna: number): GameObjects.Container {
        const { x, y } = this.centro(fila, columna);
        const lado = this.tamano * 0.8;
        const caja = this.escena.add.container(x, y);

        caja.add([
            new Recuadro(this.escena, 0, 0, lado, lado, 4, true).dibujar(COLOR.PANEL, COLOR.PACMAN, 2),
            this.escena.add.text(0, 1, '?', estiloTexto(Math.round(lado * 0.55), COLOR.PACMAN)).setOrigin(0.5),
        ]);

        this.escena.tweens.add({ targets: caja, y: y - 3, duration: 500, yoyo: true, repeat: -1, ease: 'Sine.easeInOut' });

        return caja;
    }
}
