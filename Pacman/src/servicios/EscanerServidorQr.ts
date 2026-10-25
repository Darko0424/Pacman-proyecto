import { Capacitor } from '@capacitor/core';
import {
    BarcodeFormat,
    BarcodeScanner,
} from '@capacitor-mlkit/barcode-scanning';
import type { PluginListenerHandle } from '@capacitor/core';
import { normalizarUrlApi, PUERTO_API } from './Configuracion';

// El QR de emparejamiento tiene un esquema propio para no confundirlo con
// cualquier dirección web que aparezca delante de la cámara.
export function leerUrlServidorQr(contenido: string): string | null {
    try {
        const enlace = new URL(contenido.trim());

        if (enlace.protocol !== 'pacman:' || enlace.hostname !== 'servidor' ||
            enlace.hash || enlace.searchParams.size !== 1) {
            return null;
        }

        const urlApi = normalizarUrlApi(enlace.searchParams.get('url') ?? '');

        if (!urlApi) {
            return null;
        }

        const direccion = new URL(urlApi);

        return direccion.protocol === 'http:' && direccion.port === String(PUERTO_API)
            ? urlApi
            : null;
    } catch {
        return null;
    }
}

export async function escanearServidorQr(senal?: AbortSignal): Promise<string | null> {
    if (Capacitor.getPlatform() !== 'android') {
        throw new Error('El escáner QR está disponible en la app de Android. Puedes escribir la IP aquí.');
    }

    const { supported } = await BarcodeScanner.isSupported();

    if (!supported) {
        throw new Error('Este dispositivo no tiene una cámara compatible. Escribe la IP de la PC.');
    }

    let { camera } = await BarcodeScanner.checkPermissions();

    if (camera !== 'granted') {
        ({ camera } = await BarcodeScanner.requestPermissions());
    }

    if (camera !== 'granted') {
        throw new Error('Permite usar la cámara para escanear el QR, o escribe la IP de la PC.');
    }

    if (senal?.aborted) {
        return null;
    }

    const visor = crearVisor();
    const cancelar = visor.querySelector('button')!;
    let oyenteCodigos: PluginListenerHandle | undefined;
    let oyenteErrores: PluginListenerHandle | undefined;
    let finalizar: (valor: string | null) => void = () => {};
    let fallar: (error: Error) => void = () => {};

    const resultado = new Promise<string | null>((resolver, rechazar) => {
        finalizar = resolver;
        fallar = rechazar;
    });
    const cancelarEscaneo = () => finalizar(null);
    const alPulsarTecla = (evento: KeyboardEvent) => {
        if (evento.key === 'Escape') {
            cancelarEscaneo();
        }
    };

    cancelar.addEventListener('click', cancelarEscaneo);
    document.addEventListener('keydown', alPulsarTecla);
    senal?.addEventListener('abort', cancelarEscaneo, { once: true });
    document.body.append(visor);
    document.documentElement.classList.add('escaneando-servidor-qr');
    document.body.classList.add('escaneando-servidor-qr');

    try {
        oyenteCodigos = await BarcodeScanner.addListener('barcodesScanned', ({ barcodes }) => {
            const codigo = barcodes.find((barcode) => barcode.format === BarcodeFormat.QrCode);

            if (codigo) {
                finalizar(codigo.rawValue ?? codigo.displayValue);
            }
        });
        oyenteErrores = await BarcodeScanner.addListener('scanError', ({ message }) => {
            fallar(new Error(message || 'La cámara no pudo leer el QR.'));
        });

        await BarcodeScanner.startScan({ formats: [BarcodeFormat.QrCode] });
        return await resultado;
    } finally {
        await Promise.allSettled([
            BarcodeScanner.stopScan(),
            oyenteCodigos?.remove(),
            oyenteErrores?.remove(),
        ]);
        cancelar.removeEventListener('click', cancelarEscaneo);
        document.removeEventListener('keydown', alPulsarTecla);
        senal?.removeEventListener('abort', cancelarEscaneo);
        visor.remove();
        document.documentElement.classList.remove('escaneando-servidor-qr');
        document.body.classList.remove('escaneando-servidor-qr');
    }
}

function crearVisor(): HTMLDivElement {
    const visor = document.createElement('div');
    visor.className = 'visor-servidor-qr';

    const indicacion = document.createElement('p');
    indicacion.textContent = 'Apunta al QR que aparece en la PC';

    const cancelar = document.createElement('button');
    cancelar.type = 'button';
    cancelar.textContent = 'CANCELAR';

    visor.append(indicacion, cancelar);
    return visor;
}
