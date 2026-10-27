import { ANCHO_INTERFAZ } from '../ui/PantallaAdaptable';
import { Scene } from 'phaser';
import {
    CODIGO_CORREO_NO_VERIFICADO,
    ERROR_SIN_CONEXION,
    actualizarUsuarioSesion,
    borrarSesion,
    guardarSesion,
    iniciarSesion,
    obtenerSesion,
    validarSesion,
} from '../../servicios/ClienteApi';
import { Boton } from '../ui/Boton';
import { crearCampo, habilitarCampos } from '../ui/CampoTexto';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { crearPanel } from '../ui/Panel';
import { DatosVerificacion } from './EscenaVerificacion';
import { Aviso, continuarConSesion, dibujarPantallaSesion } from './FlujoSesion';

const AYUDA_SIN_CONEXION = 'No se pudo conectar con el servidor.\nRevisa SERVIDOR y que la API esté encendida.';

export class EscenaLogin extends Scene {
    private campoUsuario: HTMLInputElement;
    private campoContrasena: HTMLInputElement;
    private aviso: Aviso;
    private botonEntrar: Boton;
    private botonCrear: Boton;
    private botonServidor: Boton;
    private ocupado = false;
    private avisoInicial = '';

    constructor() {
        super('EscenaLogin');
    }

    init(datos?: { aviso?: string }): void {
        this.avisoInicial = datos?.aviso ?? '';
    }

    create(): void {
        const centro = ANCHO_INTERFAZ / 2;

        this.ocupado = false;

        dibujarPantallaSesion(this, 'INICIAR SESION');
        crearPanel(this, centro - 260, 220, 520, 250);

        this.add.text(centro - 220, 250, 'Correo o usuario', estiloTexto(16, COLOR.MURO_CLARO)).setOrigin(0, 1);
        this.campoUsuario = crearCampo(this, centro, 300, {
            ejemplo: 'tucorreo@gmail.com',
            autocompletar: 'username',
            largoMaximo: 100,
            alEnter: () => this.campoContrasena.focus(),
        });

        this.add.text(centro - 220, 360, 'Contraseña', estiloTexto(16, COLOR.MURO_CLARO)).setOrigin(0, 1);
        this.campoContrasena = crearCampo(this, centro, 410, {
            tipo: 'password',
            ejemplo: '********',
            autocompletar: 'current-password',
            largoMaximo: 128,
            alEnter: () => this.entrar(),
        });

        this.aviso = new Aviso(this, 510);

        if (this.avisoInicial) {
            this.aviso.error(this.avisoInicial);
        }

        this.botonEntrar = new Boton(this, centro, 580, 'ENTRAR', {
            ancho: 300,
            color: COLOR.PACMAN,
            alPulsar: () => this.entrar(),
        });

        this.botonCrear = new Boton(this, centro, 660, 'CREAR CUENTA', {
            ancho: 300,
            alPulsar: () => this.scene.start('EscenaRegistro'),
        });

        // Arriba a la derecha: casi no se usa, pero hace falta al cambiar de red.
        // Cabe aunque la pantalla solo muestre 700 de ancho (formularios).
        this.botonServidor = new Boton(this, centro + 256, 56, 'SERVIDOR', {
            ancho: 150,
            alto: 44,
            tamanoTexto: 12,
            color: COLOR.GRIS_CLARO,
            alPulsar: () => this.scene.start('EscenaServidor'),
        });

        this.campoUsuario.focus();
        this.revisarSesionGuardada();
    }

    private async revisarSesionGuardada(): Promise<void> {
        if (!obtenerSesion()) {
            return;
        }

        this.setOcupado(true, 'Verificando sesión...');

        const respuesta = await validarSesion();

        if (!this.sys.isActive()) {
            return;
        }

        if (respuesta.success && respuesta.data) {
            actualizarUsuarioSesion(respuesta.data);
            continuarConSesion(this, respuesta.data);
            return;
        }

        // Sin servidor no se sabe si la sesión sigue vigente: se conserva para
        // volver a revisarla al elegir otro servidor
        this.setOcupado(false);

        if (respuesta.error === ERROR_SIN_CONEXION) {
            this.aviso.error(AYUDA_SIN_CONEXION);
            return;
        }

        borrarSesion();
    }

    private async entrar(): Promise<void> {
        if (this.ocupado) {
            return;
        }

        const usuario = this.campoUsuario.value.trim();
        const contrasena = this.campoContrasena.value;

        if (!usuario || !contrasena) {
            this.aviso.error('Escribe tu correo y tu contraseña.');
            return;
        }

        this.setOcupado(true, 'Conectando...');

        const respuesta = await iniciarSesion(usuario, contrasena);

        if (!this.sys.isActive()) {
            return;
        }

        if (respuesta.codigo === CODIGO_CORREO_NO_VERIFICADO && respuesta.data) {
            const datos: DatosVerificacion = {
                correo: respuesta.data.usuario.correoElectronico,
                codigoEnviado: false,
            };

            this.scene.start('EscenaVerificacion', datos);
            return;
        }

        if (!respuesta.success || !respuesta.data) {
            this.setOcupado(false);
            this.aviso.error(respuesta.error === ERROR_SIN_CONEXION
                ? AYUDA_SIN_CONEXION
                : respuesta.error || 'No se pudo iniciar sesión.');
            this.campoContrasena.value = '';
            this.campoContrasena.focus();
            return;
        }

        guardarSesion(respuesta.data);
        continuarConSesion(this, respuesta.data.usuario);
    }

    private setOcupado(ocupado: boolean, texto = ''): void {
        this.ocupado = ocupado;
        this.botonEntrar.setHabilitado(!ocupado);
        this.botonCrear.setHabilitado(!ocupado);
        // El jugador debe poder corregir la dirección incluso si la red no responde.
        this.botonServidor.setHabilitado(true);
        habilitarCampos([this.campoUsuario, this.campoContrasena], !ocupado);
        this.aviso.info(texto);
    }
}
