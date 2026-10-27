import { ANCHO_INTERFAZ } from '../ui/PantallaAdaptable';
import { Scene } from 'phaser';
import { Capacitor } from '@capacitor/core';
import { escanearServidorQr, leerUrlServidorQr } from '../../servicios/EscanerServidorQr';
import {
    URL_API_PREDETERMINADA,
    guardarUrlApi,
    normalizarUrlApi,
    obtenerUrlApi,
} from '../../servicios/Configuracion';
import { conexionTiempoReal } from '../../servicios/ConexionTiempoReal';
import { Boton } from '../ui/Boton';
import { crearCampo, habilitarCampos } from '../ui/CampoTexto';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { crearPanel } from '../ui/Panel';
import { Aviso, dibujarPantallaSesion } from './FlujoSesion';

// En el campo cabe poco: "http://" se sobrentiende (normalizarUrlApi lo agrega)
function sinEsquema(url: string): string {
    return url.replace(/^http:\/\//, '');
}

// Dirección de la PC donde corre PacmanApi. Se guarda en el dispositivo, así
// la app funciona en cualquier red sin volver a compilar el APK.
export class EscenaServidor extends Scene {
    private campoServidor: HTMLInputElement;
    private aviso: Aviso;
    private botones: Boton[] = [];
    private ocupado = false;

    constructor() {
        super('EscenaServidor');
    }

    create(): void {
        const centro = ANCHO_INTERFAZ / 2;
        const esAndroid = Capacitor.getPlatform() === 'android';

        this.ocupado = false;

        dibujarPantallaSesion(this, 'SERVIDOR');
        crearPanel(this, centro - 260, 220, 520, 250);

        this.add.text(centro - 220, 250, 'IP o URL del túnel', estiloTexto(16, COLOR.MURO_CLARO)).setOrigin(0, 1);
        this.campoServidor = crearCampo(this, centro, 300, {
            ejemplo: '192.168.1.100',
            largoMaximo: 200,
            alEnter: () => this.guardar(),
        });
        this.campoServidor.inputMode = 'url';
        this.campoServidor.value = sinEsquema(obtenerUrlApi());

        this.add
            .text(centro, 375,
                'Usa la red local o un túnel de Visual Studio.\nEn la PC abre:\nhttp://localhost:5253/conectar',
                estiloTexto(10, COLOR.GRIS_CLARO))
            .setOrigin(0.5)
            .setAlign('center')
            .setLineSpacing(8);
        this.add
            .text(centro, 430, `Predeterminada: ${URL_API_PREDETERMINADA}`, estiloTexto(10, COLOR.GRIS))
            .setOrigin(0.5);

        this.aviso = new Aviso(this, esAndroid ? 490 : 510);

        this.botones = [
            ...(esAndroid ? [new Boton(this, centro, 550, 'ESCANEAR QR', {
                ancho: 360,
                color: COLOR.CELESTE,
                alPulsar: () => this.escanearQr(),
            })] : []),
            new Boton(this, centro, esAndroid ? 620 : 580, 'GUARDAR SERVIDOR', {
                ancho: 360,
                color: COLOR.PACMAN,
                alPulsar: () => this.guardar(),
            }),
            new Boton(this, centro - 155, esAndroid ? 700 : 660, 'PREDETERMINADA', {
                ancho: 290,
                tamanoTexto: 12,
                alPulsar: () => this.restablecer(),
            }),
            new Boton(this, centro + 155, esAndroid ? 700 : 660, 'VOLVER', {
                ancho: 290,
                alPulsar: () => this.volver(),
            }),
        ];
    }

    private async escanearQr(): Promise<void> {
        if (this.ocupado) {
            return;
        }

        const cancelar = new AbortController();
        const alSalir = () => cancelar.abort();
        this.events.once('shutdown', alSalir);
        this.setOcupado(true, 'Abriendo cámara...');

        try {
            const contenido = await escanearServidorQr(cancelar.signal);

            if (!this.sys.isActive()) {
                return;
            }

            this.setOcupado(false);

            if (contenido === null) {
                this.aviso.info('Escaneo cancelado. Puedes escribir la IP.');
                return;
            }

            const url = leerUrlServidorQr(contenido);

            if (!url) {
                this.aviso.error('Ese QR no es de Pac-Man. Usa el que muestra la PC.');
                return;
            }

            this.campoServidor.value = sinEsquema(url);
            await this.guardar();
        } catch (error) {
            if (this.sys.isActive()) {
                this.setOcupado(false);
                this.aviso.error(error instanceof Error ? error.message : 'No se pudo abrir la cámara.');
            }
        } finally {
            this.events.off('shutdown', alSalir);
        }
    }

    private async guardar(): Promise<void> {
        if (this.ocupado) {
            return;
        }

        const url = normalizarUrlApi(this.campoServidor.value);

        if (!url) {
            this.aviso.error('Escribe una IP o la URL HTTPS del túnel.');
            return;
        }

        this.campoServidor.value = sinEsquema(url);
        this.setOcupado(true, 'Guardando servidor...');
        await this.cambiarServidor(url === URL_API_PREDETERMINADA ? null : url, 'Servidor guardado.');
    }

    // La dirección predeterminada se restaura aunque la API esté apagada.
    private async restablecer(): Promise<void> {
        if (this.ocupado) {
            return;
        }

        this.campoServidor.value = sinEsquema(URL_API_PREDETERMINADA);
        this.setOcupado(true);
        await this.cambiarServidor(null, 'Se usará la dirección predeterminada.');
    }

    private async cambiarServidor(url: string | null, mensaje: string): Promise<void> {
        guardarUrlApi(url);

        // La conexión de tiempo real abierta apunta al servidor anterior
        await conexionTiempoReal.desconectar();

        if (!this.sys.isActive()) {
            return;
        }

        this.aviso.exito(mensaje);
        this.time.delayedCall(700, () => this.volver());
    }

    private volver(): void {
        this.scene.start('EscenaLogin');
    }

    private setOcupado(ocupado: boolean, texto = ''): void {
        this.ocupado = ocupado;
        this.botones.forEach((boton) => boton.setHabilitado(!ocupado));
        habilitarCampos([this.campoServidor], !ocupado);
        this.aviso.info(texto);
    }
}
