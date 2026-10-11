// Catálogo de las hojas de sprites (public/assets/sprites): qué textura y qué
// cuadro corresponde a cada personaje, fruta y punto del juego.

export const TEXTURA = {
    PACMAN_AMARILLO: 'pacman-amarillo',
    PACMAN_VERDE: 'pacman-verde',
    FANTASMAS: 'fantasmas',
    // Misma imagen que FANTASMAS, cortada a 16x16 para sacar las caras
    CARAS: 'caras',
    ITEMS: 'items',
    TILES: 'tiles',
    LABERINTO_CLASICO: 'laberinto-clasico',
} as const;

export const TAMANO_ACTOR = 32;
export const TAMANO_ITEM = 16;
export const TAMANO_CASILLA = 16;

export type Direccion = 'ARRIBA' | 'ABAJO' | 'IZQUIERDA' | 'DERECHA';

// ---------- Pac-Man ----------

// La clave es personajes.color en la base de datos
const TEXTURA_PACMAN: Record<string, string> = {
    AMARILLO: TEXTURA.PACMAN_AMARILLO,
    VERDE: TEXTURA.PACMAN_VERDE,
};

export const COLORES_PACMAN = Object.keys(TEXTURA_PACMAN);

export const CUADROS_PACMAN = {
    QUIETO: 1,
    COMER: [0, 1, 2, 1],
    MORIR: { inicio: 4, fin: 10 },
};

export function texturaPacMan(color: string): string {
    return TEXTURA_PACMAN[color] ?? TEXTURA.PACMAN_AMARILLO;
}

// ---------- Fantasmas ----------

// La hoja tiene una fila de 4 cuadros por color; la cara va aparte, encima del cuerpo
export const COLUMNAS_FANTASMA = 4;

interface AspectoFantasma {
    fila: number;
    cara: number;
}

// La clave es personajes.color en la base de datos
const ASPECTO_FANTASMA: Record<string, AspectoFantasma> = {
    ROJO: { fila: 0, cara: 0 },     // Blinky
    CELESTE: { fila: 1, cara: 1 },  // Inky
    ROSA: { fila: 2, cara: 2 },     // Pinky
    NARANJA: { fila: 3, cara: 3 },
};

export const COLORES_FANTASMA = Object.keys(ASPECTO_FANTASMA);

export const FILA_FANTASMA_ASUSTADO = 8;
export const FILA_FANTASMA_BLANCO = 9;

// En la textura CARAS (8 columnas de 16x16) las caras y las bolas son las dos últimas filas
const PRIMER_CUADRO_CARA = 160;
const PRIMER_CUADRO_BOLA = 168;

export function aspectoFantasma(color: string): AspectoFantasma {
    return ASPECTO_FANTASMA[color] ?? ASPECTO_FANTASMA.ROJO;
}

export function cuadroCuerpoFantasma(fila: number, columna = 0): number {
    return fila * COLUMNAS_FANTASMA + columna;
}

export function cuadroCaraFantasma(color: string): number {
    return PRIMER_CUADRO_CARA + aspectoFantasma(color).cara;
}

// Fantasma comido: solo queda la cara, que vuelve a la casa
export function cuadroBolaFantasma(color: string): number {
    return PRIMER_CUADRO_BOLA + aspectoFantasma(color).cara;
}

// ---------- Frutas y puntos ----------

export const CUADRO_ITEM = {
    CEREZA: 0,
    FRESA: 1,
    NARANJA: 2,
    MANZANA: 3,
    MELON: 4,
    NAVE: 5,
    CAMPANA: 6,
    LLAVE: 7,
    PUNTO: 8,
    PUNTO_PODER: 9,
} as const;

// nombre es frutas.nombre en la base de datos ("Cereza", "Fresa"...)
export function cuadroFruta(nombre: string): number {
    const clave = nombre.trim().toUpperCase() as keyof typeof CUADRO_ITEM;

    return CUADRO_ITEM[clave] ?? CUADRO_ITEM.CEREZA;
}
