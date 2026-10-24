import { ANCHO_INTERFAZ } from '../ui/PantallaAdaptable';
import { GameObjects, Scene } from 'phaser';
import { RankingEntrada, RolRanking } from '../../modelos/ranking-entrada.model';
import { RankingTotalEntrada } from '../../modelos/ranking-total-entrada.model';
import {
    listarRanking,
    listarRankingTotal,
    obtenerMiPosicion,
    obtenerMiPosicionTotal,
    obtenerSesion,
} from '../../servicios/ClienteApi';
import { Boton } from '../ui/Boton';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { crearPanel, dibujarMarco } from '../ui/Panel';
import { Aviso } from './FlujoSesion';

type PestanaRanking = RolRanking | 'TOTAL';
type EntradaRanking = RankingEntrada | RankingTotalEntrada;

const FILAS_POR_PAGINA = 5;

export class EscenaRanking extends Scene {
    private pestana: PestanaRanking = 'PACMAN';
    private entradas: EntradaRanking[] = [];
    private pagina = 0;
    private solicitud = 0;
    private cargando = false;
    private filas: GameObjects.Container;
    private posicion: GameObjects.Text;
    private encabezadoPuntos: GameObjects.Text;
    private encabezadoCantidad: GameObjects.Text;
    private indicadorPagina: GameObjects.Text;
    private aviso: Aviso;
    private anterior: Boton;
    private siguiente: Boton;
    private actualizar: Boton;
    private pestanas: { tipo: PestanaRanking; boton: Boton }[] = [];

    constructor() {
        super('EscenaRanking');
    }

    create(): void {
        if (!obtenerSesion()) {
            this.scene.start('EscenaLogin');
            return;
        }

        this.entradas = [];
        this.pagina = 0;
        this.pestana = 'PACMAN';
        this.cargando = false;

        const centro = ANCHO_INTERFAZ / 2;

        dibujarMarco(this);
        this.add
            .text(centro, 64, 'RANKING', estiloTexto(30, COLOR.PACMAN))
            .setOrigin(0.5);
        this.add
            .text(
                centro,
                106,
                'Solo partidas multijugador finalizadas',
                estiloTexto(11, COLOR.GRIS_CLARO)
            )
            .setOrigin(0.5);

        this.pestanas = [
            {
                tipo: 'PACMAN',
                texto: 'PAC-MAN',
                color: COLOR.PACMAN
            },
            {
                tipo: 'FANTASMA',
                texto: 'FANTASMA',
                color: COLOR.ROSA
            },
            {
                tipo: 'TOTAL',
                texto: 'TOTAL',
                color: COLOR.VERDE
            },
        ].map((opcion, indice) => ({
            tipo: opcion.tipo as PestanaRanking,
            boton: new Boton(
                this,
                centro + (indice - 1) * 270,
                160,
                opcion.texto,
                {
                    ancho: 240,
                    color: opcion.color,
                    alPulsar: () => this.cargar(opcion.tipo as PestanaRanking),
                }
            ),
        }));

        crearPanel(this, 102, 208, 820, 76, COLOR.PACMAN);
        this.posicion = this.add
            .text(centro, 246, '', estiloTexto(11, COLOR.PACMAN))
            .setOrigin(0.5)
            .setAlign('center')
            .setWordWrapWidth(770);

        this.add.text(126, 310, 'POS.', estiloTexto(10, COLOR.GRIS_CLARO));
        this.add.text(218, 310, 'JUGADOR', estiloTexto(10, COLOR.GRIS_CLARO));
        this.encabezadoPuntos = this.add
            .text(700, 310, '', estiloTexto(10, COLOR.GRIS_CLARO))
            .setOrigin(1, 0);
        this.encabezadoCantidad = this.add
            .text(896, 310, '', estiloTexto(10, COLOR.GRIS_CLARO))
            .setOrigin(1, 0);
        this.filas = this.add.container(0, 0);
        this.indicadorPagina = this.add
            .text(centro, 616, '', estiloTexto(10, COLOR.GRIS_CLARO))
            .setOrigin(0.5);
        this.aviso = new Aviso(this, 580);

        new Boton(this, 170, 680, 'VOLVER', {
            ancho: 200,
            color: COLOR.GRIS_CLARO,
            alPulsar: () => this.scene.start('EscenaMenu'),
        });
        this.anterior = new Boton(this, 404, 680, 'ANTERIOR', {
            ancho: 200,
            alPulsar: () => this.cambiarPagina(-1),
        });
        this.siguiente = new Boton(this, 632, 680, 'SIGUIENTE', {
            ancho: 200,
            alPulsar: () => this.cambiarPagina(1),
        });
        this.actualizar = new Boton(this, 858, 680, 'ACTUALIZAR', {
            ancho: 210,
            tamanoTexto: 12,
            alPulsar: () => this.cargar(this.pestana),
        });

        // Una respuesta de una visita anterior no debe modificar la escena nueva.
        this.events.once('shutdown', () => this.solicitud++);
        void this.cargar(this.pestana);
    }

    private async cargar(pestana: PestanaRanking): Promise<void> {
        const solicitud = ++this.solicitud;

        this.pestana = pestana;
        this.pagina = 0;
        this.entradas = [];
        this.cargando = true;
        this.posicion.setText('Consultando tu posición...');
        this.encabezadoPuntos.setText(
            pestana === 'TOTAL'
                ? 'PUNTOS TOTALES'
                : 'MEJOR PUNTAJE'
        );
        this.encabezadoCantidad.setText(
            pestana === 'TOTAL'
                ? 'PARTIDAS'
                : 'NIVELES'
        );
        this.pestanas.forEach(({ tipo, boton }) =>
            boton.setHabilitado(tipo !== pestana)
        );
        this.aviso.info('Cargando ranking...');
        this.dibujarFilas();

        const [listado, posicion] = await Promise.all([
            pestana === 'TOTAL'
                ? listarRankingTotal()
                : listarRanking(pestana),
            pestana === 'TOTAL'
                ? obtenerMiPosicionTotal()
                : obtenerMiPosicion(pestana),
        ]);

        // También descarta respuestas de pestañas que ya no están seleccionadas.
        if (!this.sys.isActive() || solicitud !== this.solicitud) {
            return;
        }

        this.cargando = false;

        if (listado.success && listado.data) {
            this.entradas = listado.data;
            this.aviso.limpiar();
        } else {
            this.aviso.error(listado.error || 'No se pudo cargar el ranking. Pulsa ACTUALIZAR.');
        }

        if (posicion.success && posicion.data) {
            const entrada = posicion.data;
            const puesto = entrada.posicion === null ? '-' : `#${entrada.posicion}`;

            this.posicion.setText(
                `TU POSICION: ${puesto}   |   ${this.puntos(entrada).toLocaleString('es-CR')} PTS`
            );
        } else {
            this.posicion.setText(posicion.error || 'Todavía no tienes una posición en este ranking.');
        }

        this.dibujarFilas(listado.success);
    }

    private dibujarFilas(listadoDisponible = false): void {
        this.filas.removeAll(true);

        const inicio = this.pagina * FILAS_POR_PAGINA;
        const idUsuario = obtenerSesion()?.usuario.idUsuario;

        this.entradas
            .slice(inicio, inicio + FILAS_POR_PAGINA)
            .forEach((entrada, indice) => {
                const y = 340 + indice * 44;
                const propia = entrada.idUsuario === idUsuario;
                const color = propia ? COLOR.PACMAN : COLOR.BLANCO;
                const puesto = entrada.posicion === null ? '-' : `#${entrada.posicion}`;
                const cantidad = 'nivelesEnRol' in entrada
                    ? entrada.nivelesEnRol
                    : entrada.partidasJugadas;
                const nombre = entrada.nombreUsuario.length > 23
                    ? `${entrada.nombreUsuario.slice(0, 22)}…`
                    : entrada.nombreUsuario;

                this.filas.add([
                    crearPanel(this, 102, y, 820, 38, propia ? COLOR.PACMAN : COLOR.MURO),
                    this.add
                        .text(126, y + 19, puesto, estiloTexto(11, color))
                        .setOrigin(0, 0.5),
                    this.add
                        .text(218, y + 19, nombre, estiloTexto(11, color))
                        .setOrigin(0, 0.5),
                    this.add
                        .text(
                            700,
                            y + 19,
                            this.puntos(entrada).toLocaleString('es-CR'),
                            estiloTexto(11, color)
                        )
                        .setOrigin(1, 0.5),
                    this.add
                        .text(
                            896,
                            y + 19,
                            cantidad.toLocaleString('es-CR'),
                            estiloTexto(11, color)
                        )
                        .setOrigin(1, 0.5),
                ]);
            });

        if (listadoDisponible && this.entradas.length === 0) {
            this.filas.add(
                this.add
                    .text(
                        ANCHO_INTERFAZ / 2,
                        440,
                        'Aún no hay jugadores en este ranking.',
                        estiloTexto(13, COLOR.GRIS_CLARO)
                    )
                    .setOrigin(0.5)
            );
        }

        const paginas = Math.max(1, Math.ceil(this.entradas.length / FILAS_POR_PAGINA));

        this.indicadorPagina.setText(
            this.cargando
                ? ''
                : `PAGINA ${this.pagina + 1}/${paginas}`
        );
        this.anterior.setHabilitado(!this.cargando && this.pagina > 0);
        this.siguiente.setHabilitado(!this.cargando && this.pagina + 1 < paginas);
        this.actualizar.setHabilitado(!this.cargando);
    }

    private cambiarPagina(cambio: number): void {
        const pagina = this.pagina + cambio;
        const paginas = Math.ceil(this.entradas.length / FILAS_POR_PAGINA);

        if (this.cargando || pagina < 0 || pagina >= paginas) {
            return;
        }

        this.pagina = pagina;
        this.dibujarFilas(true);
    }

    private puntos(entrada: EntradaRanking): number {
        return 'puntosTotales' in entrada
            ? entrada.puntosTotales
            : entrada.mejorPuntuacion ?? 0;
    }
}
