import { ANCHO_INTERFAZ } from '../ui/PantallaAdaptable';
import { GameObjects, Scene, Time } from 'phaser';
import { actualizarUsuarioSesion, asignarNombre, obtenerSesion, sugerirNombres } from '../../servicios/ClienteApi';
import { Boton } from '../ui/Boton';
import { crearCampo } from '../ui/CampoTexto';
import { COLOR, css, estiloTexto } from '../ui/Paleta';
import { Aviso, ESCENA_PRINCIPAL, continuarConSesion, dibujarPantallaSesion } from './FlujoSesion';


const PATRON_NOMBRE = /^[A-Za-z0-9_.]{3,50}$/;
const ESPERA_ESCRITURA_MS = 400;
const AYUDA = 'Letras, números, "_" y "."';

export class EscenaNombre extends Scene {
    private campoNombre: HTMLInputElement;
    private estado: GameObjects.Text;
    private sugerencias: GameObjects.Container;
    private aviso: Aviso;
    private botonConfirmar: Boton;
    private espera: Time.TimerEvent | null = null;
    private consulta = 0;
    private disponible = false;
    private ocupado = false;
    private nombresSugeridos: string[] = [];

    constructor() {
        super('EscenaNombre');
    }

    create(): void {
        const centro = ANCHO_INTERFAZ / 2;

        this.espera = null;
        this.consulta = 0;
        this.disponible = false;
        this.ocupado = false;
        this.nombresSugeridos = [];

        dibujarPantallaSesion(this, 'ELIGE TU NOMBRE');
        this.add.text(centro, 210, 'Así te verán los demás jugadores', estiloTexto(12, COLOR.GRIS_CLARO)).setOrigin(0.5);

        this.campoNombre = crearCampo(this, centro, 280, {
            ejemplo: 'pepe',
            largoMaximo: 50,
            alEnter: () => this.confirmar(),
        });
        this.campoNombre.addEventListener('input', () => this.alEscribir());

        this.estado = this.add.text(centro, 340, AYUDA, estiloTexto(11, COLOR.GRIS)).setOrigin(0.5);
        this.sugerencias = this.add.container(0, 0);
        this.scale.on('resize', this.ajustarSugerencias, this);
        this.events.once('shutdown', () => this.scale.off('resize', this.ajustarSugerencias, this));
        this.aviso = new Aviso(this, 580);

        // Si ya tenía nombre (viene del menú) puede volver sin cambiarlo
        const puedeVolver = obtenerSesion()?.usuario.nombreDefinido === true;

        this.botonConfirmar = new Boton(this, puedeVolver ? centro + 170 : centro, 660, 'CONFIRMAR', {
            ancho: 300,
            color: COLOR.PACMAN,
            alPulsar: () => this.confirmar(),
        });
        this.botonConfirmar.setHabilitado(false);

        if (puedeVolver) {
            new Boton(this, centro - 170, 660, 'VOLVER', {
                ancho: 300,
                color: COLOR.GRIS_CLARO,
                alPulsar: () => this.scene.start(ESCENA_PRINCIPAL),
            });
        }
        this.events.once('shutdown', () => this.espera?.remove());

        this.campoNombre.focus();
    }

    private alEscribir(): void {
        this.espera?.remove();
        this.espera = null;
        this.disponible = false;
        this.botonConfirmar.setHabilitado(false);
        this.sugerencias.removeAll(true);
        this.nombresSugeridos = [];
        this.aviso.limpiar();

        const numero = ++this.consulta;
        const nombre = this.campoNombre.value.trim();

        if (!nombre) {
            this.mostrarEstado(AYUDA, COLOR.GRIS);
            return;
        }

        if (!PATRON_NOMBRE.test(nombre)) {
            this.mostrarEstado(
                nombre.length < 3 ? 'Mínimo 3 caracteres' : 'Solo letras sin tilde, números, "_" y "."',
                COLOR.ROSA,
            );
            return;
        }

        this.mostrarEstado('Revisando...', COLOR.GRIS_CLARO);
        this.espera = this.time.delayedCall(ESPERA_ESCRITURA_MS, () => this.consultar(nombre, numero));
    }

    private async consultar(nombre: string, numero: number): Promise<void> {
        const respuesta = await sugerirNombres(nombre);

       
        if (!this.sys.isActive() || numero !== this.consulta) {
            return;
        }

        if (!respuesta.success || !respuesta.data) {
            this.mostrarEstado(respuesta.error || 'No se pudo revisar el nombre.', COLOR.ROSA);
            return;
        }

        this.disponible = respuesta.data.disponible;
        this.botonConfirmar.setHabilitado(this.disponible);

        if (this.disponible) {
            this.mostrarEstado('Disponible', COLOR.VERDE);
            return;
        }

        this.mostrarEstado('Ese nombre ya está ocupado. Prueba con:', COLOR.ROSA);
        this.mostrarSugerencias(respuesta.data.sugerencias);
    }

    
    private mostrarSugerencias(nombres: string[]): void {
        const centro = ANCHO_INTERFAZ / 2;
        const anchoDisponible = Math.min(900, this.scale.width - 48);
        const separacion = anchoDisponible / 3;
        const anchoBoton = Math.min(280, separacion - 16);
        const escala = this.scale.displaySize.width / this.scale.width;
        const saltoFila = Math.max(70, 44 / escala + 16);
        const posiciones = [
            [centro - separacion, 410], [centro, 410], [centro + separacion, 410],
            [centro - separacion / 2, 410 + saltoFila], [centro + separacion / 2, 410 + saltoFila],
        ];
        this.nombresSugeridos = nombres.slice(0, posiciones.length);

        this.nombresSugeridos.forEach((nombre, i) => {
            const [x, y] = posiciones[i];

            const boton = new Boton(this, x, y, nombre, {
                ancho: anchoBoton,
                alto: 48,
                tamanoTexto: 10,
                color: COLOR.VERDE,
                alPulsar: () => this.elegirSugerencia(nombre),
            });

            this.sugerencias.add(boton);
        });
    }

    private ajustarSugerencias(): void {
        this.sugerencias.removeAll(true);
        this.mostrarSugerencias(this.nombresSugeridos);
    }

    private elegirSugerencia(nombre: string): void {
        this.campoNombre.value = nombre;
        this.campoNombre.focus();
        this.alEscribir();
    }

    private async confirmar(): Promise<void> {
        if (this.ocupado || !this.disponible) {
            return;
        }

        this.ocupado = true;
        this.botonConfirmar.setHabilitado(false);
        this.campoNombre.disabled = true;
        this.aviso.info('Guardando...');

        const respuesta = await asignarNombre(this.campoNombre.value.trim());

        if (!this.sys.isActive()) {
            return;
        }

        if (!respuesta.success || !respuesta.data) {
            this.ocupado = false;
            this.campoNombre.disabled = false;
            this.alEscribir();
            this.aviso.error(respuesta.error || 'No se pudo guardar el nombre.');
            return;
        }

        actualizarUsuarioSesion(respuesta.data);
        continuarConSesion(this, respuesta.data);
    }

    private mostrarEstado(texto: string, color: number): void {
        this.estado.setColor(css(color)).setText(texto);
    }
}
