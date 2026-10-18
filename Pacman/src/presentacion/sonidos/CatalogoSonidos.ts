// Catálogo de los sonidos y la clave con la que se
// cargan en Phaser y el archivo de cada uno. Los reproduce el cliente cuando
// llegan los eventos del servidor 

export const SONIDO = {
    WAKA: 'waka',
    COMER_FANTASMA: 'comer-fantasma',
    COMER_FRUTA: 'comer-fruta',
    MUERTE: 'muerte',
    VIDA: 'vida',
    NIVEL_COMPLETADO: 'nivel-completado',
    INTERMISION: 'intermision',
} as const;

export type ClaveSonido = (typeof SONIDO)[keyof typeof SONIDO];

// Cada clave coincide con el nombre del archivo
export const ARCHIVOS_SONIDO: ClaveSonido[] = Object.values(SONIDO);
