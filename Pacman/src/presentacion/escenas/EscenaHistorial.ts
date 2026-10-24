import { ANCHO_INTERFAZ } from '../ui/PantallaAdaptable';
import { GameObjects, Scene } from 'phaser';
import { HistorialEntrada } from '../../modelos/historial-entrada.model';
import { HistorialRonda } from '../../modelos/historial-ronda.model';
import { ProgresoJugador } from '../../modelos/progreso-jugador.model';
import {
    listarHistorial,
    listarRondasHistorial,
    obtenerProgreso,
    obtenerSesion,
} from '../../servicios/ClienteApi';
import { Boton } from '../ui/Boton';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { crearPanel, dibujarMarco } from '../ui/Panel';
import { Aviso } from './FlujoSesion';

const PARTIDAS_POR_PAGINA = 3;
const RONDAS_POR_PAGINA = 5;

export class EscenaHistorial extends Scene {
    private partidas: HistorialEntrada[] = [];
    private rondas: HistorialRonda[] = [];
    private seleccionada: HistorialEntrada | null = null;
    private paginaPartidas = 0;
    private paginaRondas = 0;
    private solicitud = 0;
    private cargando = false;
    private listadoDisponible = false;
    private rondasDisponibles = false;
    private filas: GameObjects.Container;
    private progreso: GameObjects.Container;
    private tituloLista: GameObjects.Text;
    private indicadorPagina: GameObjects.Text;
    private aviso: Aviso;
    private avisoProgreso: GameObjects.Text;
    private volver: Boton;
    private anterior: Boton;
    private siguiente: Boton;
    private actualizar: Boton;

    constructor() {
        super('EscenaHistorial');
    }

    create(): void {
        if (!obtenerSesion()) {
            this.scene.start('EscenaLogin');
            return;
        }

        this.partidas = [];
        this.rondas = [];
        this.seleccionada = null;
        this.paginaPartidas = 0;
        this.paginaRondas = 0;
        this.cargando = false;
        this.listadoDisponible = false;
        this.rondasDisponibles = false;

        const centro = ANCHO_INTERFAZ / 2;

        dibujarMarco(this);
        this.add
            .text(centro, 64, 'HISTORIAL', estiloTexto(30, COLOR.PACMAN))
            .setOrigin(0.5);
        this.add
            .text(
                centro,
                108,
                'Tus partidas y tu progreso',
                estiloTexto(12, COLOR.GRIS_CLARO)
            )
            .setOrigin(0.5);
        crearPanel(this, 102, 140, 820, 98, COLOR.PACMAN);
        this.progreso = this.add.container(0, 0);
        this.avisoProgreso = this.add
            .text(centro, 189, '', estiloTexto(10, COLOR.GRIS_CLARO))
            .setOrigin(0.5)
            .setAlign('center')
            .setWordWrapWidth(770);
        this.tituloLista = this.add
            .text(centro, 270, 'PARTIDAS', estiloTexto(14, COLOR.CELESTE))
            .setOrigin(0.5);
        this.filas = this.add.container(0, 0);
        this.aviso = new Aviso(this, 630);
        this.indicadorPagina = this.add
            .text(centro, 654, '', estiloTexto(10, COLOR.GRIS_CLARO))
            .setOrigin(0.5);

        this.volver = new Boton(this, 170, 704, 'VOLVER', {
            ancho: 200,
            color: COLOR.GRIS_CLARO,
            alPulsar: () => this.regresar(),
        });
        this.anterior = new Boton(this, 404, 704, 'ANTERIOR', {
            ancho: 200,
            alPulsar: () => this.cambiarPagina(-1),
        });
        this.siguiente = new Boton(this, 632, 704, 'SIGUIENTE', {
            ancho: 200,
            alPulsar: () => this.cambiarPagina(1),
        });
        this.actualizar = new Boton(this, 858, 704, 'ACTUALIZAR', {
            ancho: 210,
            tamanoTexto: 12,
            alPulsar: () => this.actualizarDatos(),
        });

        this.events.once('shutdown', () => this.solicitud++);
        void this.cargarHistorial();
    }

    private async cargarHistorial(): Promise<void> {
        const solicitud = ++this.solicitud;

        this.cargando = true;
        this.listadoDisponible = false;
        this.partidas = [];
        this.progreso.removeAll(true);
        this.avisoProgreso.setText('Consultando progreso...');
        this.aviso.info('Cargando partidas...');
        this.dibujarLista();

        const [historial, progreso] = await Promise.all([
            listarHistorial(),
            obtenerProgreso()
        ]);

        if (!this.esSolicitudActual(solicitud)) {
            return;
        }

        this.cargando = false;

        if (progreso.success && progreso.data) {
            this.avisoProgreso.setText('');
            this.dibujarProgreso(progreso.data);
        } else {
            this.avisoProgreso.setText(progreso.error || 'No se pudo cargar tu progreso. Pulsa ACTUALIZAR.');
        }

        if (historial.success && historial.data) {
            this.partidas = historial.data;
            this.listadoDisponible = true;
            this.paginaPartidas = Math.min(this.paginaPartidas, this.totalPaginas() - 1);
            this.aviso.limpiar();
        } else {
            this.aviso.error(historial.error || 'No se pudo cargar el historial. Pulsa ACTUALIZAR.');
        }

        this.dibujarLista();
    }

    private async cargarRondas(partida: HistorialEntrada): Promise<void> {
        const solicitud = ++this.solicitud;

        this.seleccionada = partida;
        this.rondas = [];
        this.rondasDisponibles = false;
        this.cargando = true;
        this.aviso.info('Cargando niveles...');
        this.dibujarLista();

        const respuesta = await listarRondasHistorial(partida.idPartida);

        if (!this.esSolicitudActual(solicitud)) {
            return;
        }

        this.cargando = false;

        if (respuesta.success && respuesta.data) {
            this.rondas = respuesta.data;
            this.rondasDisponibles = true;
            this.paginaRondas = Math.min(this.paginaRondas, this.totalPaginas() - 1);
            this.aviso.limpiar();
        } else {
            this.aviso.error(respuesta.error || 'No se pudo cargar el detalle. Pulsa ACTUALIZAR.');
        }

        this.dibujarLista();
    }

    private dibujarProgreso(progreso: ProgresoJugador): void {
        const valores = [
            {
                titulo: 'PARTIDAS',
                valor: progreso.partidasJugadas
            },
            {
                titulo: 'PUNTOS TOTALES',
                valor: progreso.puntosTotales
            },
            {
                titulo: 'MEJOR PUNTAJE',
                valor: progreso.mejorPuntuacion
            },
            {
                titulo: 'NIVEL MAXIMO',
                valor: progreso.nivelMaximoAlcanzado
            },
        ];

        valores.forEach((dato, indice) => {
            const x = 204 + indice * 205;

            this.progreso.add([
                this.add
                    .text(x, 166, dato.titulo, estiloTexto(9, COLOR.GRIS_CLARO))
                    .setOrigin(0.5),
                this.add
                    .text(
                        x,
                        205,
                        dato.valor.toLocaleString('es-CR'),
                        estiloTexto(16, COLOR.PACMAN)
                    )
                    .setOrigin(0.5),
            ]);
        });
    }

    private dibujarLista(): void {
        this.filas.removeAll(true);

        if (this.seleccionada) {
            const partida = this.seleccionada;

            this.tituloLista.setText(`NIVELES DE SALA ${partida.codigoSala}`);
            this.volver.setTexto('PARTIDAS');

            const inicio = this.paginaRondas * RONDAS_POR_PAGINA;

            this.rondas
                .slice(inicio, inicio + RONDAS_POR_PAGINA)
                .forEach((ronda, indice) => {
                    this.dibujarRonda(ronda, 300 + indice * 63);
                });

            if (!this.cargando && this.rondasDisponibles && this.rondas.length === 0) {
                this.mostrarVacio('Esta partida todavía no tiene niveles guardados.');
            }
        } else {
            this.tituloLista.setText('PARTIDAS');
            this.volver.setTexto('VOLVER');

            const inicio = this.paginaPartidas * PARTIDAS_POR_PAGINA;

            this.partidas
                .slice(inicio, inicio + PARTIDAS_POR_PAGINA)
                .forEach((partida, indice) => {
                    this.dibujarPartida(partida, 300 + indice * 104);
                });

            if (!this.cargando && this.listadoDisponible && this.partidas.length === 0) {
                this.mostrarVacio('Todavía no has jugado partidas.');
            }
        }

        const pagina = this.seleccionada ? this.paginaRondas : this.paginaPartidas;
        const paginas = this.totalPaginas();

        this.indicadorPagina.setText(
            this.cargando
                ? ''
                : `PAGINA ${pagina + 1}/${paginas}`
        );
        this.anterior.setHabilitado(!this.cargando && pagina > 0);
        this.siguiente.setHabilitado(!this.cargando && pagina + 1 < paginas);
        this.actualizar.setHabilitado(!this.cargando);
    }

    private dibujarPartida(partida: HistorialEntrada, y: number): void {
        const estado = partida.estado === 'EN_CURSO'
            ? 'EN CURSO'
            : partida.estado;
        const modo = partida.modo === 'CONTRA_IA'
            ? 'CONTRA IA'
            : 'MULTIJUGADOR';
        const color = partida.estado === 'ABANDONADA' ? COLOR.ROSA : COLOR.VERDE;

        this.filas.add([
            crearPanel(this, 102, y, 820, 94),
            this.add.text(
                120,
                y + 15,
                `SALA ${partida.codigoSala}  |  ${estado}`,
                estiloTexto(11, color)
            ),
            this.add.text(
                120,
                y + 38,
                `${modo}  ·  ${partida.dificultad}  ·  ${this.fecha(partida.fechaInicio)}`,
                estiloTexto(9, COLOR.GRIS_CLARO)
            ),
            this.add.text(
                120,
                y + 59,
                `${partida.puntuacion.toLocaleString('es-CR')} PTS  ·  NIVEL ${partida.nivelAlcanzado}  ·  ${partida.nivelesGanados}/${partida.nivelesJugados} GANADOS`,
                estiloTexto(10, COLOR.BLANCO)
            ),
            this.add.text(
                120,
                y + 78,
                partida.abandono
                    ? 'Abandonaste esta partida'
                    : `${partida.objetosUsados} objetos usados`,
                estiloTexto(
                    9,
                    partida.abandono
                        ? COLOR.ROSA
                        : COLOR.GRIS_CLARO
                )
            ),
            new Boton(this, 816, y + 48, 'VER NIVELES', {
                ancho: 176,
                alto: 48,
                tamanoTexto: 11,
                alPulsar: () => {
                    this.paginaRondas = 0;
                    void this.cargarRondas(partida);
                },
            }),
        ]);
    }

    private dibujarRonda(ronda: HistorialRonda, y: number): void {
        const resultado = ronda.abandono
            ? 'ABANDONO'
            : ronda.gano === 1
                ? 'VICTORIA'
                : ronda.gano === 0
                    ? 'DERROTA'
                    : 'SIN GANADOR';
        const color = ronda.abandono
            ? COLOR.ROSA
            : ronda.gano === 1
                ? COLOR.VERDE
                : COLOR.GRIS_CLARO;
        const rol = ronda.rol === 'PACMAN' ? 'PAC-MAN' : 'FANTASMA';
        const capturas = ronda.rol === 'PACMAN'
            ? `${ronda.fantasmasComidos} fantasmas comidos`
            : `${ronda.pacmansAtrapados} Pac-Man atrapados`;
        const nombreNivel = ronda.nivel.length > 22
            ? `${ronda.nivel.slice(0, 21)}…`
            : ronda.nivel;

        this.filas.add([
            crearPanel(this, 102, y, 820, 57),
            this.add.text(
                118,
                y + 9,
                `N${ronda.numeroNivel} ${nombreNivel}`,
                estiloTexto(10, COLOR.CELESTE)
            ),
            this.add.text(440, y + 9, rol, estiloTexto(10, COLOR.PACMAN)),
            this.add
                .text(
                    636,
                    y + 9,
                    `${ronda.puntuacion.toLocaleString('es-CR')} PTS`,
                    estiloTexto(10, COLOR.BLANCO)
                )
                .setOrigin(0.5, 0),
            this.add
                .text(904, y + 9, resultado, estiloTexto(10, color))
                .setOrigin(1, 0),
            this.add.text(
                118,
                y + 34,
                `${ronda.personaje}  ·  ${capturas}  ·  ${ronda.objetosUsados} objetos`,
                estiloTexto(9, COLOR.GRIS_CLARO)
            ),
        ]);
    }

    private mostrarVacio(texto: string): void {
        this.filas.add(
            this.add
                .text(
                    ANCHO_INTERFAZ / 2,
                    444,
                    texto,
                    estiloTexto(13, COLOR.GRIS_CLARO)
                )
                .setOrigin(0.5)
                .setAlign('center')
                .setWordWrapWidth(760)
        );
    }

    private actualizarDatos(): void {
        if (this.cargando) {
            return;
        }

        if (this.seleccionada) {
            void this.cargarRondas(this.seleccionada);
        } else {
            void this.cargarHistorial();
        }
    }

    private cambiarPagina(cambio: number): void {
        const pagina = (this.seleccionada ? this.paginaRondas : this.paginaPartidas) + cambio;

        if (this.cargando || pagina < 0 || pagina >= this.totalPaginas()) {
            return;
        }

        if (this.seleccionada) {
            this.paginaRondas = pagina;
        } else {
            this.paginaPartidas = pagina;
        }

        this.dibujarLista();
    }

    private regresar(): void {
        if (!this.seleccionada) {
            this.scene.start('EscenaMenu');
            return;
        }

        // Volver al listado invalida incluso una consulta de detalle en curso.
        this.solicitud++;
        this.cargando = false;
        this.seleccionada = null;
        this.aviso.limpiar();
        this.dibujarLista();
    }

    private totalPaginas(): number {
        const cantidad = this.seleccionada ? this.rondas.length : this.partidas.length;
        const porPagina = this.seleccionada ? RONDAS_POR_PAGINA : PARTIDAS_POR_PAGINA;

        return Math.max(1, Math.ceil(cantidad / porPagina));
    }

    private esSolicitudActual(solicitud: number): boolean {
        return this.sys.isActive() && solicitud === this.solicitud;
    }

    private fecha(valor: string): string {
        const fecha = new Date(valor);

        return Number.isNaN(fecha.getTime())
            ? 'Fecha no disponible'
            : fecha.toLocaleString('es-CR', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
            });
    }
}
