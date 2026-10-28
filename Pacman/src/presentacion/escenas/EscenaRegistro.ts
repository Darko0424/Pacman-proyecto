import { ANCHO_INTERFAZ } from '../ui/PantallaAdaptable';
import { Scene } from 'phaser';
import { registrar } from '../../servicios/ClienteApi';
import { Boton } from '../ui/Boton';
import { crearCampo, habilitarCampos } from '../ui/CampoTexto';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { crearPanel } from '../ui/Panel';
import { DatosVerificacion } from './EscenaVerificacion';
import { Aviso, dibujarPantallaSesion } from './FlujoSesion';


const PATRON_CORREO = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const LARGO_MAXIMO_CORREO = 100;
const LARGO_MINIMO_CONTRASENA = 8;
const LARGO_MAXIMO_CONTRASENA = 128;
const LARGO_MINIMO_USUARIO_CORREO = 3;
export const REQUISITOS_CONTRASENA =
    `Mínimo ${LARGO_MINIMO_CONTRASENA} caracteres, con mayúscula, minúscula, número y símbolo.`;

// Mismas reglas que UsuarioLN.ValidarContrasena en la API
export function validarContrasena(contrasena: string, usuarioCorreo: string): string | null {
    if (contrasena.length < LARGO_MINIMO_CONTRASENA || contrasena.length > LARGO_MAXIMO_CONTRASENA) {
        return `La contraseña debe tener entre ${LARGO_MINIMO_CONTRASENA} y ${LARGO_MAXIMO_CONTRASENA} caracteres.`;
    }

    if (/\s/u.test(contrasena)) {
        return 'La contraseña no puede tener espacios.';
    }

    if (!/\p{Lu}/u.test(contrasena) || !/\p{Ll}/u.test(contrasena)) {
        return 'La contraseña debe tener al menos una mayúscula y una minúscula.';
    }

    if (!/\p{Nd}/u.test(contrasena)) {
        return 'La contraseña debe tener al menos un número.';
    }

    if (!/[^\p{L}\p{N}]/u.test(contrasena)) {
        return 'La contraseña debe tener al menos un símbolo (por ejemplo ! # $ . _).';
    }

    if (usuarioCorreo.length >= LARGO_MINIMO_USUARIO_CORREO &&
        contrasena.toLowerCase().includes(usuarioCorreo.toLowerCase())) {
        return 'La contraseña no puede contener tu correo.';
    }

    return null;
}

export class EscenaRegistro extends Scene {
    private campoCorreo: HTMLInputElement;
    private campoContrasena: HTMLInputElement;
    private campoConfirmar: HTMLInputElement;
    private aviso: Aviso;
    private botonCrear: Boton;
    private botonVolver: Boton;
    private ocupado = false;

    constructor() {
        super('EscenaRegistro');
    }

    create(): void {
        const centro = ANCHO_INTERFAZ / 2;

        this.ocupado = false;

        dibujarPantallaSesion(this, 'CREAR CUENTA');
        crearPanel(this, centro - 260, 210, 520, 300);

        this.add.text(centro - 220, 235, 'Correo electrónico', estiloTexto(16, COLOR.MURO_CLARO)).setOrigin(0, 1);
        this.campoCorreo = crearCampo(this, centro, 280, {
            tipo: 'email',
            ejemplo: 'tucorreo@gmail.com',
            autocompletar: 'email',
            largoMaximo: LARGO_MAXIMO_CORREO,
            alEnter: () => this.campoContrasena.focus(),
        });

        this.add.text(centro - 220, 330, 'Contraseña', estiloTexto(16, COLOR.MURO_CLARO)).setOrigin(0, 1);
        this.campoContrasena = crearCampo(this, centro, 375, {
            tipo: 'password',
            ejemplo: 'Ej. Pacman#2026',
            autocompletar: 'new-password',
            largoMaximo: LARGO_MAXIMO_CONTRASENA,
            alEnter: () => this.campoConfirmar.focus(),
        });

        this.add.text(centro - 220, 425, 'Repite la contraseña', estiloTexto(16, COLOR.MURO_CLARO)).setOrigin(0, 1);
        this.campoConfirmar = crearCampo(this, centro, 470, {
            tipo: 'password',
            ejemplo: '********',
            autocompletar: 'new-password',
            largoMaximo: LARGO_MAXIMO_CONTRASENA,
            alEnter: () => this.registrarse(),
        });

        this.aviso = new Aviso(this, 545);
        this.aviso.info(REQUISITOS_CONTRASENA);

        this.botonCrear = new Boton(this, centro, 610, 'CREAR CUENTA', {
            ancho: 300,
            color: COLOR.PACMAN,
            alPulsar: () => this.registrarse(),
        });

        this.botonVolver = new Boton(this, centro, 685, 'VOLVER', {
            ancho: 300,
            color: COLOR.GRIS_CLARO,
            alPulsar: () => this.scene.start('EscenaLogin'),
        });

        this.campoCorreo.focus();
    }

    private async registrarse(): Promise<void> {
        if (this.ocupado) {
            return;
        }

        const correo = this.campoCorreo.value.trim().toLowerCase();
        const contrasena = this.campoContrasena.value;
        const error = this.validar(correo, contrasena, this.campoConfirmar.value);

        if (error) {
            this.aviso.error(error);
            return;
        }

        this.setOcupado(true, 'Creando cuenta...');

        const respuesta = await registrar(correo, contrasena);

        if (!this.sys.isActive()) {
            return;
        }

        if (!respuesta.success) {
            this.setOcupado(false);
            this.aviso.error(respuesta.error || 'No se pudo crear la cuenta.');
            return;
        }

        const datos: DatosVerificacion = {
            correo,
            codigoEnviado: true,
            aviso: respuesta.error || undefined,
        };

        this.scene.start('EscenaVerificacion', datos);
    }

    private validar(correo: string, contrasena: string, confirmacion: string): string | null {
        if (!correo || !contrasena) {
            return 'Completa todos los campos.';
        }

        if (correo.length > LARGO_MAXIMO_CORREO || !PATRON_CORREO.test(correo)) {
            return 'El correo no es válido.';
        }

        const errorContrasena = validarContrasena(contrasena, correo.split('@')[0]);

        if (errorContrasena) {
            return errorContrasena;
        }

        if (contrasena !== confirmacion) {
            return 'Las contraseñas no coinciden.';
        }

        return null;
    }

    private setOcupado(ocupado: boolean, texto = ''): void {
        this.ocupado = ocupado;
        this.botonCrear.setHabilitado(!ocupado);
        this.botonVolver.setHabilitado(!ocupado);
        habilitarCampos([this.campoCorreo, this.campoContrasena, this.campoConfirmar], !ocupado);
        this.aviso.info(texto);
    }
}