import { Direccion } from '../modelos/tiempo-real/enums';
import { EstadoPartidaDto } from '../modelos/tiempo-real/estado-partida.dto';

// El servidor manda unos 20 estados por segundo. Para que el movimiento se vea
// continuo se dibuja un poco en el pasado, siempre entre dos estados recibidos.
const RETRASO_SEG = 0.1;
const MAXIMO_ESTADOS = 30;

// Si entre dos estados un personaje salta más que esto (reaparición), no se
// interpola: se dibuja directo en su lugar nuevo.
const SALTO_MAXIMO_CASILLAS = 1.5;

// [fila, columna] que suma cada dirección
const PASO: Record<Direccion, [number, number]> = {
    [Direccion.Ninguna]: [0, 0],
    [Direccion.Arriba]: [-1, 0],
    [Direccion.Abajo]: [1, 0],
    [Direccion.Izquierda]: [0, -1],
    [Direccion.Derecha]: [0, 1],
};

export interface Posicion {
    fila: number;
    columna: number;
}

interface Desplazamiento extends Posicion {
    direccion: Direccion;
    avance: number;
}

// Casilla actual más lo que lleva recorrido hacia la siguiente
export function posicionVisible(actor: Desplazamiento): Posicion {
    const [df, dc] = PASO[actor.direccion];

    return { fila: actor.fila + df * actor.avance, columna: actor.columna + dc * actor.avance };
}

export function interpolarPosicion(a: Desplazamiento, b: Desplazamiento, fraccion: number): Posicion {
    const desde = posicionVisible(a);
    const hasta = posicionVisible(b);

    if (Math.abs(hasta.fila - desde.fila) + Math.abs(hasta.columna - desde.columna) > SALTO_MAXIMO_CASILLAS) {
        return fraccion < 0.5 ? desde : hasta;
    }

    return {
        fila: desde.fila + (hasta.fila - desde.fila) * fraccion,
        columna: desde.columna + (hasta.columna - desde.columna) * fraccion,
    };
}

export interface Muestra {
    anterior: EstadoPartidaDto;
    siguiente: EstadoPartidaDto;
    // 0 = anterior, 1 = siguiente
    fraccion: number;
    // Tiempo de simulación que se está dibujando
    tiempoSimulacion: number;
}

// Guarda los últimos estados y da, para cada cuadro, los dos que rodean el
// instante a dibujar. Los tiempos son segundos.
export class Interpolacion {
    private estados: EstadoPartidaDto[] = [];

    // Reloj local menos tiempo de simulación; se ajusta con cada estado
    private desfase: number | null = null;

    get ultimo(): EstadoPartidaDto | null {
        return this.estados[this.estados.length - 1] ?? null;
    }

    reiniciar(): void {
        this.estados = [];
        this.desfase = null;
    }

    agregar(estado: EstadoPartidaDto, ahora: number): void {
        const ultimo = this.ultimo;

        if (ultimo && ultimo.numeroNivel !== estado.numeroNivel) {
            this.reiniciar();
        } else if (ultimo && estado.tiempoSimulacionSegundos <= ultimo.tiempoSimulacionSegundos) {
            // Repetido (por ejemplo, al reconectar) o llegó tarde
            return;
        }

        this.estados.push(estado);

        if (this.estados.length > MAXIMO_ESTADOS) {
            this.estados.shift();
        }

        // Se queda con la llegada más rápida y se acerca despacio a las demás,
        // así un estado que llega tarde no hace saltar el dibujo
        const medido = ahora - estado.tiempoSimulacionSegundos;
        this.desfase = this.desfase === null || medido < this.desfase
            ? medido
            : this.desfase * 0.98 + medido * 0.02;
    }

    muestrear(ahora: number): Muestra | null {
        const total = this.estados.length;

        if (total === 0 || this.desfase === null) {
            return null;
        }

        const primero = this.estados[0];
        const ultimo = this.estados[total - 1];
        const objetivo = ahora - this.desfase - RETRASO_SEG;

        if (objetivo <= primero.tiempoSimulacionSegundos) {
            return { anterior: primero, siguiente: primero, fraccion: 0, tiempoSimulacion: primero.tiempoSimulacionSegundos };
        }

        if (objetivo >= ultimo.tiempoSimulacionSegundos) {
            return { anterior: ultimo, siguiente: ultimo, fraccion: 0, tiempoSimulacion: ultimo.tiempoSimulacionSegundos };
        }

        for (let i = total - 1; i > 0; i--) {
            const anterior = this.estados[i - 1];

            if (anterior.tiempoSimulacionSegundos <= objetivo) {
                const siguiente = this.estados[i];
                const tramo = siguiente.tiempoSimulacionSegundos - anterior.tiempoSimulacionSegundos;

                return {
                    anterior,
                    siguiente,
                    fraccion: (objetivo - anterior.tiempoSimulacionSegundos) / tramo,
                    tiempoSimulacion: objetivo,
                };
            }
        }

        return null;
    }
}
