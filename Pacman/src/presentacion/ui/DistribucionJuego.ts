import { Zona } from './HUD';

export interface DistribucionJuego {
    izquierda: Zona;
    derecha: Zona;
    mapa: Zona;
}

export function calcularDistribucionJuego(ancho: number, alto: number, escala = 1): DistribucionJuego {
    const margen = 16;
    const anchoPanel = Math.min(Math.max(304, 156 / escala), (ancho - 448 - margen * 4) / 2);
    const altoPanel = Math.min(744, alto - 24);
    const arriba = (alto - altoPanel) / 2;

    return {
        izquierda: { x: margen, y: arriba, ancho: anchoPanel, alto: altoPanel },
        derecha: {
            x: ancho - margen - anchoPanel,
            y: arriba,
            ancho: anchoPanel,
            alto: altoPanel,
        },
        mapa: {
            x: anchoPanel + margen * 2,
            y: arriba,
            ancho: ancho - anchoPanel * 2 - margen * 4,
            alto: altoPanel,
        },
    };
}
