// La paleta del juego sacada de los píxeles de las hojas de sprites para que los
// menús combinen con el laberinto y los personajes. Phaser usa los números para
// figuras y las cadenas CSS para textos.

export const COLOR = {
    // Laberinto
    FONDO: 0x272736,
    MARCO: 0x473b78,
    PANEL: 0x322947,
    MURO: 0x4b5bab,
    MURO_CLARO: 0x4da6ff,
    PUERTA: 0xff6b97,

    // Personajes
    PACMAN: 0xffe478,
    PACMAN_SOMBRA: 0xffb570,
    VERDE: 0x8fde5d,
    VERDE_OSCURO: 0x3ca370,
    ROJO: 0xeb564b,
    ROJO_OSCURO: 0x964253,
    NARANJA: 0xff9166,
    ROSA: 0xff6b97,
    CELESTE: 0x4da6ff,

    GRIS: 0x7e7e8f,
    GRIS_CLARO: 0xc2c2d1,
    BLANCO: 0xffffff,
} as const;

// El mismo color como cadena CSS (#rrggbb)
export function css(color: number): string {
    return `#${color.toString(16).padStart(6, '0')}`;
}

// Fuente pixelada empaquetada con @fontsource (funciona sin internet).
// No trae mayúsculas con tilde: los textos en mayúsculas van sin tilde.
export const FUENTE = '"Press Start 2P", monospace';

export function estiloTexto(
    tamano: number,
    color: number = COLOR.BLANCO,
): Phaser.Types.GameObjects.Text.TextStyle {
    return { fontFamily: FUENTE, fontSize: tamano, color: css(color) };
}
