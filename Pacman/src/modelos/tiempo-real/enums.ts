// Enums del hub de juego. Viajan con su valor numérico, igual que en
// PacmanDominio (EstadoSala, ModoPartida, Bando, Direccion...).

export const EstadoSala = {
    Lobby: 1,
    Jugando: 2,
    // Vuelta breve al lobby entre niveles: resultados y nuevos roles
    Transicion: 3,
    Finalizada: 4,
} as const;
export type EstadoSala = (typeof EstadoSala)[keyof typeof EstadoSala];

export const ModoPartida = {
    Multijugador: 1,
    ContraIa: 2,
} as const;
export type ModoPartida = (typeof ModoPartida)[keyof typeof ModoPartida];

export const Bando = {
    PacMan: 1,
    Fantasma: 2,
} as const;
export type Bando = (typeof Bando)[keyof typeof Bando];

// Lo que acepta ElegirRol (personajes.rol); null deja el rol al sorteo
export type CodigoRol = 'PACMAN' | 'FANTASMA' | null;

export const Direccion = {
    Ninguna: 0,
    Arriba: 1,
    Abajo: 2,
    Izquierda: 3,
    Derecha: 4,
} as const;
export type Direccion = (typeof Direccion)[keyof typeof Direccion];

export const FrecuenciaCajas = {
    Ninguna: 0,
    Baja: 1,
    Normal: 2,
    Alta: 3,
} as const;
export type FrecuenciaCajas = (typeof FrecuenciaCajas)[keyof typeof FrecuenciaCajas];

export const TipoEventoJuego = {
    BolaComida: 1,
    BolaPoderComida: 2,
    FantasmaComido: 3,
    PacManAtrapado: 4,
    // Dos fantasmas chocaron
    Explosion: 5,
    // Dos Pac-Man chocaron con la colisión activada
    Rebote: 6,
    Reaparicion: 7,
    CajaAparecida: 8,
    CajaRecogida: 9,
    ObjetoUsado: 10,
    EfectoTerminado: 11,
    NivelTerminado: 12,
} as const;
export type TipoEventoJuego = (typeof TipoEventoJuego)[keyof typeof TipoEventoJuego];
