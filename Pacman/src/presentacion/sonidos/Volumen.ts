import { Scene } from 'phaser';
import { OpcionSelector } from '../ui/Selector';

const CLAVE_VOLUMEN = 'pacman.volumen';
const CLAVE_SILENCIO = 'pacman.silencio';
const VOLUMEN_POR_DEFECTO = 0.75;

export const OPCIONES_VOLUMEN: OpcionSelector<number>[] = [
    { valor: 0, texto: 'SILENCIO' },
    { valor: 0.25, texto: '25%' },
    { valor: 0.5, texto: '50%' },
    { valor: 0.75, texto: '75%' },
    { valor: 1, texto: '100%' },
];

export function obtenerVolumen(): number {
    try {
        const guardado = localStorage.getItem(CLAVE_VOLUMEN);
        const volumen = guardado === null ? NaN : Number(guardado);

        return Number.isFinite(volumen) && volumen >= 0 && volumen <= 1
            ? volumen
            : VOLUMEN_POR_DEFECTO;
    } catch {
        return VOLUMEN_POR_DEFECTO;
    }
}

export function cambiarVolumen(escena: Scene, volumen: number): void {
    escena.sound.volume = volumen;
    escena.sound.mute = false;
    guardar(CLAVE_VOLUMEN, String(volumen));
    guardar(CLAVE_SILENCIO, '0');
}

export function cambiarSilencio(escena: Scene): boolean {
    const silenciado = !escena.sound.mute;

    escena.sound.mute = silenciado;
    guardar(CLAVE_SILENCIO, silenciado ? '1' : '0');

    return silenciado;
}

export function aplicarVolumenGuardado(escena: Scene): void {
    escena.sound.volume = obtenerVolumen();

    try {
        escena.sound.mute = localStorage.getItem(CLAVE_SILENCIO) === '1';
    } catch {
        escena.sound.mute = false;
    }
}

function guardar(clave: string, valor: string): void {
    try {
        localStorage.setItem(clave, valor);
    } catch {
        // El juego sigue funcionando si el navegador bloquea el almacenamiento.
    }
}
