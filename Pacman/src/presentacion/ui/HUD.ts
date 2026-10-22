import { GameObjects, Scene } from 'phaser';
import { DESCRIPCION_EFECTO } from '../../logica/EfectosVisuales';
import { ActorConBando, nombreActor } from '../../logica/EstadoCliente';
import { TObjeto } from '../../modelos/objeto.model';
import { Bando } from '../../modelos/tiempo-real/enums';
import { EstadoPartidaDto } from '../../modelos/tiempo-real/estado-partida.dto';
import { Boton } from './Boton';
import { IndicadorEfectos } from './IndicadorEfectos';
import { COLOR, css, estiloTexto } from './Paleta';
import { Recuadro, crearPanel } from './Panel';

// Color de cada personaje (personajes.id_personaje), igual que su sprite
const COLOR_PERSONAJE: Record<number, number> = {
    1: COLOR.PACMAN,
    2: COLOR.VERDE,
    3: COLOR.ROJO,
    4: COLOR.ROSA,
    5: COLOR.CELESTE,
};

const MAXIMO_FILAS = 5;
const LARGO_NOMBRE = 9;
const ALTO_BOTON_OBJETO = 140;

interface FilaJugador {
    marca: Recuadro;
    nombre: GameObjects.Text;
    datos: GameObjects.Text;
}

export interface Zona {
    x: number;
    y: number;
    ancho: number;
    alto: number;
}

export interface OpcionesHUD {
    // Panel izquierdo: nivel, jugadores y efectos (la cruceta va abajo)
    izquierda: Zona;
    // Panel derecho: tu puntaje y el botón del objeto, donde cae el pulgar
    derecha: Zona;
    alUsarObjeto: () => void;
    alSalir: () => void;
}

// Paneles laterales de la partida
export class HUD {
    private readonly nivel: GameObjects.Text;
    private readonly rol: GameObjects.Text;
    private readonly filas: FilaJugador[] = [];
    private readonly bolas: GameObjects.Text;
    private readonly efectos: IndicadorEfectos;

    private readonly puntaje: GameObjects.Text;
    private readonly vidas: GameObjects.Text;
    private readonly botonObjeto: Boton;
    private readonly descripcionObjeto: GameObjects.Text;
    private objetoMostrado: number | null | undefined = undefined;

    constructor(escena: Scene, opciones: OpcionesHUD) {
        const izq = opciones.izquierda;
        const der = opciones.derecha;
        const margen = izq.x + 20;

        // ---------- Izquierda ----------
        crearPanel(escena, izq.x, izq.y, izq.ancho, izq.alto);

        this.nivel = escena.add.text(margen, izq.y + 22, '', estiloTexto(14, COLOR.PACMAN));
        new Boton(escena, izq.x + izq.ancho - 52, izq.y + 30, 'SALIR', {
            ancho: 80,
            alto: 32,
            tamanoTexto: 9,
            color: COLOR.ROJO,
            alPulsar: opciones.alSalir,
        });

        this.rol = escena.add.text(margen, izq.y + 52, '', estiloTexto(10, COLOR.BLANCO));

        escena.add.text(margen, izq.y + 86, 'JUGADOR    VIDAS PTS', estiloTexto(8, COLOR.GRIS));

        for (let i = 0; i < MAXIMO_FILAS; i++) {
            const filaY = izq.y + 112 + i * 30;

            this.filas.push({
                marca: new Recuadro(escena, margen + 6, filaY, 12, 12, 3, true),
                nombre: escena.add.text(margen + 20, filaY, '', estiloTexto(9, COLOR.BLANCO)).setOrigin(0, 0.5),
                datos: escena.add.text(izq.x + izq.ancho - 20, filaY, '', estiloTexto(9, COLOR.GRIS_CLARO)).setOrigin(1, 0.5),
            });
        }

        this.bolas = escena.add.text(margen, izq.y + 270, '', estiloTexto(9, COLOR.GRIS_CLARO));

        escena.add.text(margen, izq.y + 304, 'EFECTOS', estiloTexto(9, COLOR.GRIS));
        this.efectos = new IndicadorEfectos(escena, margen, izq.y + 324);

        // ---------- Derecha ----------
        const centroDer = der.x + der.ancho / 2;

        crearPanel(escena, der.x, der.y, der.ancho, der.alto);

        escena.add.text(centroDer, der.y + 30, 'TU PUNTAJE', estiloTexto(10, COLOR.GRIS)).setOrigin(0.5);
        this.puntaje = escena.add.text(centroDer, der.y + 66, '0', estiloTexto(26, COLOR.PACMAN)).setOrigin(0.5);
        this.vidas = escena.add.text(centroDer, der.y + 106, '', estiloTexto(11, COLOR.BLANCO)).setOrigin(0.5);

        const botonY = der.y + der.alto - 40 - ALTO_BOTON_OBJETO / 2;

        escena.add.text(centroDer, botonY - ALTO_BOTON_OBJETO / 2 - 46, 'OBJETO', estiloTexto(10, COLOR.GRIS)).setOrigin(0.5);
        this.descripcionObjeto = escena.add
            .text(centroDer, botonY - ALTO_BOTON_OBJETO / 2 - 22, '', estiloTexto(8, COLOR.GRIS_CLARO))
            .setOrigin(0.5);

        this.botonObjeto = new Boton(escena, centroDer, botonY, 'SIN OBJETO', {
            ancho: der.ancho - 40,
            alto: ALTO_BOTON_OBJETO,
            tamanoTexto: 13,
            color: COLOR.PACMAN,
            alPulsar: opciones.alUsarObjeto,
        }).setHabilitado(false);

        escena.add.text(centroDer, der.y + der.alto - 20, 'ESPACIO: USAR', estiloTexto(8, COLOR.GRIS)).setOrigin(0.5);
    }

    // mio: el personaje de este jugador; objetos: catálogo por idObjeto
    actualizar(
        estado: EstadoPartidaDto,
        actores: ActorConBando[],
        mio: ActorConBando | null,
        cantidadNiveles: number,
        objetos: Map<number, TObjeto>,
    ): void {
        this.nivel.setText(`NIVEL ${estado.numeroNivel}/${cantidadNiveles}`);

        if (mio) {
            const esPacMan = mio.bando === Bando.PacMan;
            this.rol.setText(esPacMan ? 'ERES PAC-MAN' : 'ERES FANTASMA')
                .setColor(css(COLOR_PERSONAJE[mio.actor.idPersonaje] ?? COLOR.BLANCO));
            this.puntaje.setText(String(mio.actor.puntuacion));
            this.vidas.setText(`VIDAS x${mio.actor.vidasRestantes}`);
        } else {
            this.rol.setText('ESPECTADOR').setColor(css(COLOR.GRIS));
        }

        this.filas.forEach((fila, i) => {
            const datos = actores[i];
            const visible = datos !== undefined;

            fila.marca.setVisible(visible);
            fila.nombre.setVisible(visible);
            fila.datos.setVisible(visible);

            if (!datos) {
                return;
            }

            const { actor } = datos;
            const color = COLOR_PERSONAJE[actor.idPersonaje] ?? COLOR.BLANCO;
            const esMio = actor.idPersonaje === mio?.actor.idPersonaje;

            fila.marca.dibujar(actor.eliminado ? null : color, color, 2);
            fila.nombre
                .setText(nombreActor(actor).slice(0, LARGO_NOMBRE))
                .setColor(css(actor.eliminado ? COLOR.GRIS : esMio ? COLOR.PACMAN : COLOR.BLANCO));
            fila.datos.setText(`x${actor.vidasRestantes} ${String(actor.puntuacion).padStart(5, ' ')}`);
        });

        this.bolas.setText(`BOLAS RESTANTES ${estado.bolasRestantes}`);

        // El botón solo se toca cuando cambia el objeto, no en cada estado
        const idObjeto = mio?.actor.idObjetoGuardado ?? null;
        if (idObjeto === this.objetoMostrado) {
            return;
        }

        this.objetoMostrado = idObjeto;
        const objeto = idObjeto === null ? undefined : objetos.get(idObjeto);

        this.botonObjeto
            .setTexto(idObjeto === null ? 'SIN OBJETO' : `USAR ${(objeto?.nombre ?? `#${idObjeto}`).toUpperCase()}`)
            .setHabilitado(idObjeto !== null);
        this.descripcionObjeto.setText(objeto ? DESCRIPCION_EFECTO[objeto.efecto] ?? '' : '');
    }

    // Se llama en cada cuadro para que la cuenta regresiva corra fluida
    actualizarEfectos(estado: EstadoPartidaDto, tiempo: number, objetos: Map<number, TObjeto>): void {
        this.efectos.actualizar(estado.efectos, tiempo, objetos);
    }
}
