import { Scene } from 'phaser';
import {
    CODIGO_CORREO_NO_VERIFICADO,
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

export class EscenaLogin extends Scene {
    private campoUsuario: HTMLInputElement;
    private campoContrasena: HTMLInputElement;
    private aviso: Aviso;
    private botonEntrar: Boton;
    private botonCrear: Boton;
    private ocupado = false;

    constructor() {
        super('EscenaLogin');
    }

    create(): void {
        const centro = this.scale.width / 2;

        this.ocupado = false;

        dibujarPantallaSesion(this, 'INICIAR SESION');
        crearPanel(this, centro - 260, 220, 520, 250);

        this.add.text(centro - 220, 250, 'Correo o usuario', estiloTexto(12, COLOR.MURO_CLARO));
        this.campoUsuario = crearCampo(this, centro, 300, {
            ejemplo: 'tucorreo@gmail.com',
            autocompletar: 'username',
            largoMaximo: 100,
            alEnter: () => this.campoContrasena.focus(),
        });

        this.add.text(centro - 220, 360, 'Contraseña', estiloTexto(12, COLOR.MURO_CLARO));
        this.campoContrasena = crearCampo(this, centro, 410, {
            tipo: 'password',
            ejemplo: '********',
            autocompletar: 'current-password',
            largoMaximo: 128,
            alEnter: () => this.entrar(),
        });

        this.aviso = new Aviso(this, 510);

        this.botonEntrar = new Boton(this, centro, 580, 'ENTRAR', {
            ancho: 300,
            color: COLOR.PACMAN,
            alPulsar: () => this.entrar(),
        });

        this.botonCrear = new Boton(this, centro, 660, 'CREAR CUENTA', {
            ancho: 300,
            alPulsar: () => this.scene.start('EscenaRegistro'),
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

        borrarSesion();
        this.setOcupado(false);
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
            this.aviso.error(respuesta.error || 'No se pudo iniciar sesión.');
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
        habilitarCampos([this.campoUsuario, this.campoContrasena], !ocupado);
        this.aviso.info(texto);
    }
}