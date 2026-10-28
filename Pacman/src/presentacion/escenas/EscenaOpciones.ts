import { Scene } from 'phaser';
import { obtenerSesion } from '../../servicios/ClienteApi';
import { OPCIONES_VOLUMEN, cambiarVolumen, obtenerVolumen } from '../sonidos/Volumen';
import { Boton } from '../ui/Boton';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { ANCHO_INTERFAZ } from '../ui/PantallaAdaptable';
import { Selector } from '../ui/Selector';
import { DatosContrasena } from './EscenaContrasena';
import { Aviso, dibujarPantallaSesion } from './FlujoSesion';

// Volumen y datos de la cuenta
export class EscenaOpciones extends Scene {
    private avisoInicial = '';

    constructor() {
        super('EscenaOpciones');
    }

    // Mensaje opcional al llegar, p. ej. después de cambiar la contraseña
    init(datos?: { aviso?: string }): void {
        this.avisoInicial = datos?.aviso ?? '';
    }

    create(): void {
        const sesion = obtenerSesion();

        if (!sesion) {
            this.scene.start('EscenaLogin');
            return;
        }

        const centro = ANCHO_INTERFAZ / 2;

        dibujarPantallaSesion(this, 'OPCIONES');
        this.add.text(centro, 220, sesion.usuario.nombreUsuario, estiloTexto(14, COLOR.PACMAN)).setOrigin(0.5);
        this.add.text(centro, 250, sesion.usuario.correoElectronico, estiloTexto(10, COLOR.GRIS_CLARO)).setOrigin(0.5);

        new Selector(this, centro - 210, 320, 420, 'VOLUMEN', OPCIONES_VOLUMEN,
            (volumen) => cambiarVolumen(this, volumen))
            .setValor(obtenerVolumen());

        new Boton(this, centro, 410, 'CAMBIAR NOMBRE', {
            ancho: 380,
            alPulsar: () => this.scene.start('EscenaNombre'),
        });

        new Boton(this, centro, 490, 'CAMBIAR CLAVE', {
            ancho: 380,
            alPulsar: () => {
                const datos: DatosContrasena = { modo: 'cambiar' };
                this.scene.start('EscenaContrasena', datos);
            },
        });

        new Boton(this, centro, 570, 'VOLVER', {
            ancho: 380,
            color: COLOR.GRIS_CLARO,
            alPulsar: () => this.scene.start('EscenaMenu'),
        });

        const aviso = new Aviso(this, 650);

        if (this.avisoInicial) {
            aviso.exito(this.avisoInicial);
        }
    }
}