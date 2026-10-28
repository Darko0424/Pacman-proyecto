import { Game, Scene } from 'phaser';

export const ANCHO_INTERFAZ = 1024;
export const ALTO_INTERFAZ = 768;

type ModoPantalla = 'interfaz' | 'formulario' | 'juego';

interface InsetsNativos {
    keyboardVisible: boolean;
    keyboardHeight: number;
}

let pantalla: PantallaAdaptable | null = null;

// Tras un toque el navegador repite el gesto como mouse (mousedown, mouseup...)
// para páginas viejas. Llega en unos cientos de milisegundos.
const EVENTOS_MOUSE_SIMULADOS = ['mousemove', 'mousedown', 'mouseup'] as const;
const ESPERA_MOUSE_SIMULADO_MS = 800;

// sourceCapabilities solo existe en Chromium (la WebView de Android lo tiene)
type EventoMouseConOrigen = MouseEvent & { sourceCapabilities?: { firesTouchEvents: boolean } | null };

interface Lienzo {
    // Tamaño del juego en unidades de Phaser
    ancho: number;
    alto: number;
    // Alto en píxeles CSS del contenedor
    altoCss: number;
    escala: number;
}

// La interfaz de referencia (ANCHO_INTERFAZ x ALTO_INTERFAZ) siempre cabe entera en la
// pantalla, así nunca aparece una barra de desplazamiento; lo que sobra a los
// lados o arriba y abajo agranda el lienzo en vez de dejar franjas. Las escenas
// centran la cámara en la interfaz de referencia.
// El tamaño del contenido y el área visible son independientes cuando aparece el teclado.
export function calcularLienzo(ancho: number, alto: number, modo: ModoPantalla): Lienzo {
    const anchoMinimo = modo === 'formulario' ? 700 : ANCHO_INTERFAZ;
    const escala = Math.min(alto / ALTO_INTERFAZ, ancho / anchoMinimo);

    return {
        ancho: ancho / escala,
        alto: alto / escala,
        altoCss: alto,
        escala,
    };
}

// Alto mínimo para tocar cómodo (44 px reales) en unidades del juego. Tiene
// tope: en celulares bajos (escala ~0.5) la interfaz de 1024 x 768 no deja
// espacio para más y los botones se encimarían
export function altoTactil(escena: Scene): number {
    const escala = escena.scale.displaySize.width / escena.scale.width;

    return Math.min(44 / escala, 72);
}

export function iniciarPantallaAdaptable(juego: Game, padre: string): void {
    pantalla = new PantallaAdaptable(juego, padre);
}

export function prepararPantalla(escena: Scene, modo: ModoPantalla): void {
    pantalla?.cambiarModo(modo);

    if (modo === 'juego') {
        return;
    }

    const centrar = () => {
        escena.cameras.main.setViewport(0, 0, escena.scale.width, escena.scale.height);
        escena.cameras.main.centerOn(ANCHO_INTERFAZ / 2, ALTO_INTERFAZ / 2);
    };

    centrar();
    escena.scale.on('resize', centrar);
    escena.events.once('shutdown', () => escena.scale.off('resize', centrar));
}

class PantallaAdaptable {
    private readonly aplicacion: HTMLElement;
    private readonly contenedor: HTMLElement;
    private modo: ModoPantalla = 'interfaz';
    private insets: InsetsNativos = { keyboardVisible: false, keyboardHeight: 0 };
    private altoSinTeclado = window.innerHeight;
    private anchoAnterior = 0;
    private lienzoAnterior: Lienzo | null = null;
    private cuadro = 0;
    private cuadroCampo = 0;
    private campoVisible: HTMLInputElement | null = null;
    private destruida = false;
    private ultimoToque = -Infinity;

    constructor(private readonly juego: Game, padre: string) {
        this.aplicacion = document.getElementById('app')!;
        this.contenedor = document.getElementById(padre)!;

        window.addEventListener('resize', this.programar);
        window.visualViewport?.addEventListener('resize', this.programar);
        window.visualViewport?.addEventListener('scroll', this.programar);
        window.addEventListener('pacman:insets', this.recibirInsets);
        document.addEventListener('focusin', this.programar);
        document.addEventListener('focusout', this.programar);
        this.aplicacion.addEventListener('scroll', this.actualizarLimites);
        this.aplicacion.addEventListener('pointerdown', this.alTocarContenido, true);
        this.aplicacion.addEventListener('wheel', this.permitirDesplazamiento);
        this.contenedor.addEventListener('touchend', this.registrarToque, { capture: true, passive: true });
        EVENTOS_MOUSE_SIMULADOS.forEach((tipo) =>
            this.contenedor.addEventListener(tipo, this.descartarMouseSimulado, true));
        juego.events.on('postrender', this.asegurarCampoVisible);
        juego.events.once('destroy', this.destruir, this);
        this.actualizar();
    }

    cambiarModo(modo: ModoPantalla): void {
        if (modo !== this.modo) {
            this.modo = modo;
            this.lienzoAnterior = null;
            this.aplicacion.scrollTop = 0;
        }

        this.aplicacion.dataset.pantalla = modo;
        this.juego.canvas.style.touchAction = modo === 'juego' ? 'none' : 'pan-y';

        if (this.juego.input.touch) {
            this.juego.input.touch.capture = modo === 'juego';
        }

        this.actualizar();
    }

    private readonly recibirInsets = (evento: Event): void => {
        this.insets = (evento as CustomEvent<InsetsNativos>).detail;
        this.programar();
    };

    private readonly programar = (): void => {
        if (this.destruida || this.cuadro) {
            return;
        }

        this.cuadro = requestAnimationFrame(() => {
            this.cuadro = 0;
            this.actualizar();
        });
    };

    private actualizar(): void {
        const viewport = window.visualViewport;
        const campo = document.activeElement;
        const escribiendo = campo instanceof HTMLInputElement && campo.matches('.campo-texto');
        const altoVisual = Math.min(window.innerHeight, viewport?.height ?? window.innerHeight);
        const teclado = this.insets.keyboardVisible ||
            (escribiendo && altoVisual < this.altoSinTeclado - 80);

        if (!teclado) {
            this.altoSinTeclado = window.innerHeight;
        }

        const altoNativo = this.insets.keyboardVisible
            ? this.altoSinTeclado - this.insets.keyboardHeight
            : window.innerHeight;
        const altoVisible = Math.max(1, Math.min(altoVisual, altoNativo));
        this.aplicacion.style.top = `${viewport?.offsetTop ?? 0}px`;
        this.aplicacion.style.height = `${altoVisible}px`;

        const estilo = getComputedStyle(this.aplicacion);
        const vertical = parseFloat(estilo.paddingTop) + parseFloat(estilo.paddingBottom);
        const horizontal = parseFloat(estilo.paddingLeft) + parseFloat(estilo.paddingRight);
        const ancho = Math.max(1, this.aplicacion.clientWidth - horizontal);
        const altoBase = Math.max(1, this.altoSinTeclado - vertical);
        // Con el teclado abierto se conserva el lienzo (si se achicara, Phaser
        // alejaría toda la pantalla); solo se desplaza #app hasta el campo activo
        const lienzo = teclado && ancho === this.anchoAnterior && this.lienzoAnterior
            ? this.lienzoAnterior
            : calcularLienzo(ancho, altoBase, this.modo);

        this.anchoAnterior = ancho;
        this.lienzoAnterior = lienzo;
        document.documentElement.style.setProperty('--escala-juego', String(lienzo.escala));
        this.contenedor.style.height = `${lienzo.altoCss}px`;
        this.juego.scale.setParentSize(ancho, lienzo.altoCss);

        if (Math.abs(this.juego.scale.width - lienzo.ancho) > 0.01 ||
            this.juego.scale.height !== lienzo.alto) {
            this.juego.scale.setGameSize(lienzo.ancho, lienzo.alto);
        }

        this.actualizarLimites();

        if (escribiendo && teclado) {
            this.campoVisible = campo;
            this.asegurarCampoVisible();
            cancelAnimationFrame(this.cuadroCampo);
            this.cuadroCampo = requestAnimationFrame(() => {
                this.cuadroCampo = 0;
                this.asegurarCampoVisible();
            });
        } else {
            this.campoVisible = null;
        }
    }

    private readonly asegurarCampoVisible = (): void => {
        const campo = this.campoVisible;

        if (!campo || !campo.isConnected || document.activeElement !== campo) {
            this.campoVisible = null;
            return;
        }

        const area = this.aplicacion.getBoundingClientRect();
        const rectangulo = campo.getBoundingClientRect();
        const estilo = getComputedStyle(this.aplicacion);
        const viewport = window.visualViewport;
        const arriba = Math.max(
            area.top + parseFloat(estilo.paddingTop), viewport?.offsetTop ?? 0
        );
        const abajo = Math.min(
            area.bottom - parseFloat(estilo.paddingBottom),
            (viewport?.offsetTop ?? 0) + (viewport?.height ?? window.innerHeight)
        );
        const margen = Math.min(12, Math.max(0, (abajo - arriba - rectangulo.height) / 2));

        if (abajo <= arriba || (rectangulo.top >= arriba + margen - 1 &&
            rectangulo.bottom <= abajo - margen + 1)) {
            return;
        }

        const delta = rectangulo.height <= abajo - arriba
            ? (rectangulo.top + rectangulo.bottom - arriba - abajo) / 2
            : rectangulo.top - arriba;
        const maximo = Math.max(0, this.aplicacion.scrollHeight - this.aplicacion.clientHeight);
        const siguiente = Math.min(maximo, Math.max(0, this.aplicacion.scrollTop + delta));

        // Solo #app se desplaza. Los ancestros DOM de Phaser conservan sus posiciones.
        if (Math.abs(siguiente - this.aplicacion.scrollTop) > 0.5) {
            this.aplicacion.scrollTop = siguiente;
            this.actualizarLimites();
        }
    };

    private readonly permitirDesplazamiento = (): void => {
        this.campoVisible = null;
    };

    private readonly alTocarContenido = (evento: Event): void => {
        if (!(evento.target instanceof HTMLInputElement) || !evento.target.matches('.campo-texto')) {
            this.permitirDesplazamiento();
        } else {
            this.programar();
        }
    };

    private readonly registrarToque = (): void => {
        this.ultimoToque = performance.now();
    };

    // Con touch.capture apagado (pantallas que se desplazan) Phaser recibe el
    // toque y además su copia de mouse sobre el canvas: cada toque contaba
    // doble. El canvas no la ve; el resto de la página sí, para mover el foco.
    private readonly descartarMouseSimulado = (evento: Event): void => {
        const origen = (evento as EventoMouseConOrigen).sourceCapabilities;
        const simulado = origen?.firesTouchEvents === true ||
            performance.now() - this.ultimoToque < ESPERA_MOUSE_SIMULADO_MS;

        if (simulado && evento.target === this.juego.canvas) {
            evento.stopPropagation();
        }
    };

    private readonly actualizarLimites = (): void => {
        this.juego.scale.updateBounds();
    };

    private destruir(): void {
        this.destruida = true;
        cancelAnimationFrame(this.cuadro);
        cancelAnimationFrame(this.cuadroCampo);
        this.campoVisible = null;
        window.removeEventListener('resize', this.programar);
        window.visualViewport?.removeEventListener('resize', this.programar);
        window.visualViewport?.removeEventListener('scroll', this.programar);
        window.removeEventListener('pacman:insets', this.recibirInsets);
        document.removeEventListener('focusin', this.programar);
        document.removeEventListener('focusout', this.programar);
        this.aplicacion.removeEventListener('scroll', this.actualizarLimites);
        this.aplicacion.removeEventListener('pointerdown', this.alTocarContenido, true);
        this.aplicacion.removeEventListener('wheel', this.permitirDesplazamiento);
        this.contenedor.removeEventListener('touchend', this.registrarToque, true);
        EVENTOS_MOUSE_SIMULADOS.forEach((tipo) =>
            this.contenedor.removeEventListener(tipo, this.descartarMouseSimulado, true));
        this.juego.events.off('postrender', this.asegurarCampoVisible);
        pantalla = null;
    }
}
