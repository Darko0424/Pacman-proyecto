import { GameObjects, Scene } from 'phaser';
import { TObjeto } from '../../modelos/objeto.model';
import { Bando } from '../../modelos/tiempo-real/enums';
import { EfectoDto } from '../../modelos/tiempo-real/estado-partida.dto';
import { COLOR, estiloTexto, ponerColor } from './Paleta';

const MAXIMO_LINEAS = 4;

// Lista de efectos activos (objetos usados) con los segundos que les quedan
export class IndicadorEfectos {
    private readonly lineas: GameObjects.Text[] = [];

    constructor(escena: Scene, x: number, y: number) {
        for (let i = 0; i < MAXIMO_LINEAS; i++) {
            this.lineas.push(escena.add.text(x, y + i * 18, '', estiloTexto(9, COLOR.BLANCO)));
        }
    }

    // objetos: catálogo por idObjeto; tiempo: segundos de simulación actuales
    actualizar(efectos: EfectoDto[], tiempo: number, objetos: Map<number, TObjeto>): void {
        this.lineas.forEach((linea, i) => {
            const efecto = efectos[i];

            if (!efecto) {
                ponerColor(linea.setText(i === 0 && efectos.length === 0 ? 'SIN EFECTOS' : ''), COLOR.GRIS);
                return;
            }

            const restante = Math.max(0, efecto.finSegundos - tiempo);
            const nombre = (objetos.get(efecto.idObjeto)?.nombre ?? efecto.tipoEfecto).toUpperCase();
            const color = efecto.bandoOrigen === Bando.PacMan ? COLOR.PACMAN : COLOR.ROSA;

            ponerColor(linea.setText(`${nombre} ${restante.toFixed(1)}s`), color);
        });
    }
}
