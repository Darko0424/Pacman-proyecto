import { Scene, Time } from 'phaser';
import {
    cambiarContrasena,
    guardarSesion,
    obtenerSesion,
    restablecerContrasena,
    solicitarCambioContrasena,
    solicitarRecuperacion,
} from '../../servicios/ClienteApi';
import { conexionTiempoReal } from '../../servicios/ConexionTiempoReal';
import { Boton } from '../ui/Boton';
import { crearCampo, habilitarCampos } from '../ui/CampoTexto';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { crearPanel } from '../ui/Panel';
import { ANCHO_INTERFAZ } from '../ui/PantallaAdaptable';
import { Aviso, continuarConSesion, dibujarPantallaSesion } from './FlujoSesion';
import { REQUISITOS_CONTRASENA, validarContrasena } from './EscenaRegistro';

// recuperar: "¿Olvidaste tu contraseña?" desde el login (sin sesión)
// cambiar: desde Opciones, con la sesión iniciada
export type ModoContrasena = 'recuperar' | 'cambiar';

// Lo que recibe con this.scene.start('EscenaContrasena', datos). En la fase
// 'pedir' se envía el código; en la fase 'codigo' se escribe junto con la
// contraseña nueva. Al pasar de una a otra la escena se reinicia.
export interface DatosContrasena {
    modo: ModoContrasena;
    fase?: 'pedir' | 'codigo';
    correo?: string;
}

const PATRON_CORREO = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const LARGO_CODIGO = 6;
const LARGO_MAXIMO_CONTRASENA = 128;
const SEGUNDOS_REENVIO = 60;

export class EscenaContrasena extends Scene {
    private datos: DatosContrasena;
    private campoCorreo: HTMLInputElement | null = null;
    private campoCodigo: HTMLInputElement | null = null;
    private campoNueva: HTMLInputElement | null = null;
    private campoConfirmar: HTMLInputElement | null = null;
    private botonPrincipal: Boton;
    private botonReenviar: Boton | null = null;
    private aviso: Aviso;
    private temporizador: Time.TimerEvent | null = null;
    private espera = 0;
    private ocupado = false;

    constructor() {
        super('EscenaContrasena');
    }

    init(datos: DatosContrasena): void {
        this.datos = { ...datos, fase: datos.fase ?? 'pedir' };
    }

    create(): void {
        // Phaser reutiliza la escena: se reinicia el estado cada vez que entra
        this.ocupado = false;
        this.espera = 0;
        this.temporizador = null;
        this.campoCorreo = null;
        this.campoCodigo = null;
        this.campoNueva = null;
        this.campoConfirmar = null;
        this.botonReenviar = null;

        const recuperar = this.datos.modo === 'recuperar';

        // Cambiar la contraseña necesita la sesión
        if (!recuperar && !obtenerSesion()) {
            this.scene.start('EscenaLogin');
            return;
        }

        dibujarPantallaSesion(this, recuperar ? 'RECUPERAR CUENTA' : 'CAMBIAR CLAVE');

        // Arriba a la izquierda, igual que SERVIDOR en el login (a la derecha)
        new Boton(this, ANCHO_INTERFAZ / 2 - 256, 56, 'VOLVER', {
            ancho: 150,
            alto: 44,
            tamanoTexto: 12,
            color: COLOR.GRIS_CLARO,
            alPulsar: () => this.volver(),
        });

        if (this.datos.fase === 'codigo') {
            this.crearFaseCodigo();
        } else {
            this.crearFasePedir();
        }

        this.events.once('shutdown', () => this.temporizador?.remove());
    }

    // ---------- Fase 1: pedir el código ----------

    private crearFasePedir(): void {
        const centro = ANCHO_INTERFAZ / 2;

        if (this.datos.modo === 'recuperar') {
            this.add
                .text(centro, 220, 'Te enviaremos un código para crear\nuna contraseña nueva.', estiloTexto(12, COLOR.GRIS_CLARO))
                .setOrigin(0.5)
                .setAlign('center');

            crearPanel(this, centro - 260, 270, 520, 130);
            this.add.text(centro - 220, 305, 'Correo electrónico', estiloTexto(16, COLOR.MURO_CLARO)).setOrigin(0, 1);
            this.campoCorreo = crearCampo(this, centro, 350, {
                tipo: 'email',
                ejemplo: 'tucorreo@gmail.com',
                autocompletar: 'email',
                largoMaximo: 100,
                alEnter: () => this.pedirCodigo(),
            });
            this.campoCorreo.value = this.datos.correo ?? '';
        } else {
            const correo = obtenerSesion()?.usuario.correoElectronico ?? '';

            this.add.text(centro, 250, 'Te enviaremos un código a', estiloTexto(12, COLOR.GRIS_CLARO)).setOrigin(0.5);
            this.add.text(centro, 290, correo, estiloTexto(14, COLOR.PACMAN)).setOrigin(0.5);
            this.add
                .text(centro, 345, 'Al cambiarla se cerrará tu cuenta\nen los demás dispositivos.', estiloTexto(11, COLOR.GRIS))
                .setOrigin(0.5)
                .setAlign('center');
        }

        this.aviso = new Aviso(this, 450);

        this.botonPrincipal = new Boton(this, centro, 540, 'ENVIAR CODIGO', {
            ancho: 320,
            color: COLOR.PACMAN,
            alPulsar: () => this.pedirCodigo(),
        });

        this.campoCorreo?.focus();
    }

    private async pedirCodigo(): Promise<void> {
        if (this.ocupado) {
            return;
        }

        const recuperar = this.datos.modo === 'recuperar';
        const correo = recuperar
            ? (this.campoCorreo?.value ?? '').trim().toLowerCase()
            : obtenerSesion()?.usuario.correoElectronico ?? '';

        if (recuperar && !PATRON_CORREO.test(correo)) {
            this.aviso.error('Escribe un correo válido.');
            return;
        }

        this.setOcupado(true, 'Enviando código...');

        const respuesta = recuperar
            ? await solicitarRecuperacion(correo)
            : await solicitarCambioContrasena();

        if (!this.sys.isActive()) {
            return;
        }

        if (!respuesta.success) {
            this.setOcupado(false);
            this.aviso.error(respuesta.error || 'No se pudo enviar el código.');
            return;
        }

        const datos: DatosContrasena = { modo: this.datos.modo, fase: 'codigo', correo };
        this.scene.restart(datos);
    }

    // ---------- Fase 2: código y contraseña nueva ----------

    private crearFaseCodigo(): void {
        const centro = ANCHO_INTERFAZ / 2;
        const explicacion = this.datos.modo === 'recuperar'
            ? 'Si el correo tiene una cuenta, enviamos un código a'
            : 'Enviamos un código a';

        this.add.text(centro, 205, explicacion, estiloTexto(11, COLOR.GRIS_CLARO)).setOrigin(0.5);
        this.add.text(centro, 232, this.datos.correo ?? '', estiloTexto(13, COLOR.PACMAN)).setOrigin(0.5);

        crearPanel(this, centro - 260, 255, 520, 310);

        this.add.text(centro - 220, 290, 'Código', estiloTexto(16, COLOR.MURO_CLARO)).setOrigin(0, 1);
        this.campoCodigo = crearCampo(this, centro, 330, {
            ejemplo: '000000',
            largoMaximo: LARGO_CODIGO,
            autocompletar: 'one-time-code',
            clase: 'campo-codigo',
            alEnter: () => this.campoNueva?.focus(),
        });
        this.campoCodigo.inputMode = 'numeric';

        // Solo números
        const campoCodigo = this.campoCodigo;
        campoCodigo.addEventListener('input', () => {
            campoCodigo.value = campoCodigo.value.replace(/\D/g, '').slice(0, LARGO_CODIGO);
        });

        this.add.text(centro - 220, 395, 'Contraseña nueva', estiloTexto(16, COLOR.MURO_CLARO)).setOrigin(0, 1);
        this.campoNueva = crearCampo(this, centro, 435, {
            tipo: 'password',
            ejemplo: 'Ej: Contra#1',
            autocompletar: 'new-password',
            largoMaximo: LARGO_MAXIMO_CONTRASENA,
            alEnter: () => this.campoConfirmar?.focus(),
        });

        this.add.text(centro - 220, 490, 'Repite la contraseña', estiloTexto(16, COLOR.MURO_CLARO)).setOrigin(0, 1);
        this.campoConfirmar = crearCampo(this, centro, 530, {
            tipo: 'password',
            ejemplo: '********',
            autocompletar: 'new-password',
            largoMaximo: LARGO_MAXIMO_CONTRASENA,
            alEnter: () => this.confirmar(),
        });

        this.aviso = new Aviso(this, 595);

        this.botonReenviar = new Boton(this, centro - 170, 665, 'REENVIAR', {
            ancho: 300,
            alPulsar: () => this.reenviar(),
        });

        this.botonPrincipal = new Boton(this, centro + 170, 665, 'CAMBIAR', {
            ancho: 300,
            color: COLOR.PACMAN,
            alPulsar: () => this.confirmar(),
        });

        //Que sepa las reglas antes de escribirla
        this.aviso.info(REQUISITOS_CONTRASENA);
        // El código se acaba de enviar: hay que esperar para pedir otro
        this.iniciarEspera();
        this.campoCodigo.focus();
    }

    private async confirmar(): Promise<void> {
        if (this.ocupado || !this.campoCodigo || !this.campoNueva || !this.campoConfirmar) {
            return;
        }

        const codigo = this.campoCodigo.value;
        const nueva = this.campoNueva.value;

        if (codigo.length !== LARGO_CODIGO) {
            this.aviso.error('Escribe los 6 números del código.');
            return;
        }

        const errorContrasena = validarContrasena(nueva, (this.datos.correo ?? '').split('@')[0]);

        if (errorContrasena) {
            this.aviso.error(errorContrasena);
            return;
        }

        if (nueva !== this.campoConfirmar.value) {
            this.aviso.error('Las contraseñas no coinciden.');
            return;
        }

        this.setOcupado(true, 'Guardando...');

        if (this.datos.modo === 'recuperar') {
            const respuesta = await restablecerContrasena(this.datos.correo ?? '', codigo, nueva);

            if (!this.sys.isActive()) {
                return;
            }

            if (!respuesta.success || !respuesta.data) {
                this.fallo(respuesta.error);
                return;
            }

            // Queda con la sesión iniciada
            guardarSesion(respuesta.data);
            continuarConSesion(this, respuesta.data.usuario);
            return;
        }

        // La API cierra todas las sesiones de la cuenta y avisa por tiempo real.
        // Se desconecta antes para que ese aviso no saque también a este dispositivo.
        await conexionTiempoReal.desconectar();

        const respuesta = await cambiarContrasena(codigo, nueva);

        if (respuesta.success && respuesta.data) {
            // Token nuevo: el anterior ya no sirve
            guardarSesion(respuesta.data);
        }

        void conexionTiempoReal.conectar();

        if (!this.sys.isActive()) {
            return;
        }

        if (!respuesta.success || !respuesta.data) {
            this.fallo(respuesta.error);
            return;
        }

        this.scene.start('EscenaOpciones', {
            aviso: 'Contraseña cambiada. Se cerró tu cuenta en los demás dispositivos.',
        });
    }

    private async reenviar(): Promise<void> {
        if (this.ocupado || this.espera > 0) {
            return;
        }

        this.setOcupado(true, 'Enviando código...');

        const respuesta = this.datos.modo === 'recuperar'
            ? await solicitarRecuperacion(this.datos.correo ?? '')
            : await solicitarCambioContrasena();

        if (!this.sys.isActive()) {
            return;
        }

        this.setOcupado(false);

        if (!respuesta.success) {
            this.aviso.error(respuesta.error || 'No se pudo enviar el código.');
            return;
        }

        this.aviso.exito('Te enviamos un código nuevo.');

        if (this.campoCodigo) {
            this.campoCodigo.value = '';
            this.campoCodigo.focus();
        }

        this.iniciarEspera();
    }

    // ---------- Estado de la pantalla ----------

    private fallo(error: string): void {
        this.setOcupado(false);
        this.aviso.error(error || 'No se pudo cambiar la contraseña.');
        this.campoCodigo?.focus();
    }

    private volver(): void {
        this.scene.start(this.datos.modo === 'recuperar' ? 'EscenaLogin' : 'EscenaOpciones');
    }

    // Cuenta regresiva del botón "Reenviar"
    private iniciarEspera(): void {
        this.temporizador?.remove();
        this.espera = SEGUNDOS_REENVIO;
        this.actualizarReenviar();

        this.temporizador = this.time.addEvent({
            delay: 1000,
            repeat: SEGUNDOS_REENVIO - 1,
            callback: () => {
                this.espera--;
                this.actualizarReenviar();
            },
        });
    }

    private actualizarReenviar(): void {
        if (!this.botonReenviar) {
            return;
        }

        this.botonReenviar.setTexto(this.espera > 0 ? `REENVIAR (${this.espera})` : 'REENVIAR');
        this.botonReenviar.setHabilitado(!this.ocupado && this.espera <= 0);
    }

    private setOcupado(ocupado: boolean, texto = ''): void {
        this.ocupado = ocupado;
        this.botonPrincipal.setHabilitado(!ocupado);

        const campos = [this.campoCorreo, this.campoCodigo, this.campoNueva, this.campoConfirmar]
            .filter((campo): campo is HTMLInputElement => campo !== null);
        habilitarCampos(campos, !ocupado);

        this.actualizarReenviar();
        this.aviso.info(texto);
    }
}