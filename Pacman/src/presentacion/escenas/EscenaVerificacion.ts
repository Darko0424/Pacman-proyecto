import { Scene, Time } from 'phaser';
import { guardarSesion, reenviarCodigo, verificarCorreo } from '../../servicios/ClienteApi';
import { Boton } from '../ui/Boton';
import { crearCampo } from '../ui/CampoTexto';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { Aviso, continuarConSesion, dibujarPantallaSesion } from './FlujoSesion';


export interface DatosVerificacion {
    correo: string;
   
    codigoEnviado: boolean;
    
    aviso?: string;
}

const LARGO_CODIGO = 6;
const SEGUNDOS_REENVIO = 60;

export class EscenaVerificacion extends Scene {
    private datos: DatosVerificacion;
    private campoCodigo: HTMLInputElement;
    private aviso: Aviso;
    private botonVerificar: Boton;
    private botonReenviar: Boton;
    private temporizador: Time.TimerEvent | null = null;
    private espera = 0;
    private ocupado = false;

    constructor() {
        super('EscenaVerificacion');
    }

    init(datos: DatosVerificacion): void {
        this.datos = datos;
    }

    create(): void {
        const centro = this.scale.width / 2;

        this.ocupado = false;
        this.espera = 0;
        this.temporizador = null;

        dibujarPantallaSesion(this, 'VERIFICA TU CORREO');

        this.add.text(centro, 240, 'Te enviamos un código de 6 dígitos a', estiloTexto(12, COLOR.GRIS_CLARO)).setOrigin(0.5);
        this.add.text(centro, 275, this.datos.correo, estiloTexto(14, COLOR.PACMAN)).setOrigin(0.5);

        this.campoCodigo = crearCampo(this, centro, 360, {
            ejemplo: '000000',
            largoMaximo: LARGO_CODIGO,
            autocompletar: 'one-time-code',
            clase: 'campo-codigo',
            alEnter: () => this.verificar(),
        });
        this.campoCodigo.inputMode = 'numeric';

        
        this.campoCodigo.addEventListener('input', () => {
            this.campoCodigo.value = this.campoCodigo.value.replace(/\D/g, '').slice(0, LARGO_CODIGO);

            if (this.campoCodigo.value.length === LARGO_CODIGO) {
                this.verificar();
            }
        });

        this.aviso = new Aviso(this, 440);

        this.botonVerificar = new Boton(this, centro, 510, 'VERIFICAR', {
            ancho: 320,
            color: COLOR.PACMAN,
            alPulsar: () => this.verificar(),
        });

        this.botonReenviar = new Boton(this, centro, 590, 'REENVIAR CODIGO', {
            ancho: 320,
            alPulsar: () => this.reenviar(),
        });

        new Boton(this, centro, 670, 'VOLVER', {
            ancho: 320,
            color: COLOR.GRIS_CLARO,
            alPulsar: () => this.scene.start('EscenaLogin'),
        });

        this.events.once('shutdown', () => this.temporizador?.remove());

        if (this.datos.aviso) {
            this.aviso.error(this.datos.aviso);
        } else if (!this.datos.codigoEnviado) {
            this.aviso.info('Si tu código ya venció, pide uno nuevo.');
        }

        if (this.datos.codigoEnviado) {
            this.iniciarEspera();
        }

        this.campoCodigo.focus();
    }

    private async verificar(): Promise<void> {
        if (this.ocupado) {
            return;
        }

        const codigo = this.campoCodigo.value;

        if (codigo.length !== LARGO_CODIGO) {
            this.aviso.error('Escribe los 6 números del código.');
            return;
        }

        this.setOcupado(true, 'Verificando...');

        const respuesta = await verificarCorreo(this.datos.correo, codigo);

        if (!this.sys.isActive()) {
            return;
        }

        if (!respuesta.success || !respuesta.data) {
            this.setOcupado(false);
            this.aviso.error(respuesta.error || 'No se pudo verificar el código.');
            this.campoCodigo.value = '';
            this.campoCodigo.focus();
            return;
        }

       
        guardarSesion(respuesta.data);
        continuarConSesion(this, respuesta.data.usuario);
    }

    private async reenviar(): Promise<void> {
        if (this.ocupado || this.espera > 0) {
            return;
        }

        this.setOcupado(true, 'Enviando código...');

        const respuesta = await reenviarCodigo(this.datos.correo);

        if (!this.sys.isActive()) {
            return;
        }

        this.setOcupado(false);

        if (!respuesta.success) {
            this.aviso.error(respuesta.error || 'No se pudo enviar el código.');
            return;
        }

        this.aviso.exito('Te enviamos un código nuevo.');
        this.campoCodigo.value = '';
        this.campoCodigo.focus();
        this.iniciarEspera();
    }

  
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
        this.botonReenviar.setTexto(this.espera > 0 ? `REENVIAR (${this.espera})` : 'REENVIAR CODIGO');
        this.botonReenviar.setHabilitado(!this.ocupado && this.espera <= 0);
    }

    private setOcupado(ocupado: boolean, texto = ''): void {
        this.ocupado = ocupado;
        this.botonVerificar.setHabilitado(!ocupado);
        this.campoCodigo.disabled = ocupado;
        this.actualizarReenviar();
        this.aviso.info(texto);
    }
}