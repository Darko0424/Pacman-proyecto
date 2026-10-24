import { GameObjects, Geom, Scene } from 'phaser';
import { EFECTO, estadoVisual } from '../../logica/EfectosVisuales';
import { EstadoCliente } from '../../logica/EstadoCliente';
import { interpolarPosicion } from '../../logica/Interpolacion';
import { Bando, Direccion, EstadoSala, TipoEventoJuego } from '../../modelos/tiempo-real/enums';
import { EstadoPartidaDto, EventoDto } from '../../modelos/tiempo-real/estado-partida.dto';
import { NivelDto } from '../../modelos/tiempo-real/nivel.dto';
import { SalaDto } from '../../modelos/tiempo-real/sala.dto';
import { TObjeto } from '../../modelos/objeto.model';
import { listarObjetos, obtenerSesion } from '../../servicios/ClienteApi';
import { conexionTiempoReal } from '../../servicios/ConexionTiempoReal';
import { GestorSonido } from '../sonidos/GestorSonido';
import { VistaActor } from '../sprites/VistaActor';
import { CASILLA_BASE, VistaLaberinto } from '../sprites/VistaLaberinto';
import { Boton } from '../ui/Boton';
import { ControlesTactiles } from '../ui/ControlesTactiles';
import { HUD } from '../ui/HUD';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { Recuadro } from '../ui/Panel';
import { prepararPantalla } from '../ui/PantallaAdaptable';
import { calcularDistribucionJuego } from '../ui/DistribucionJuego';

// GestorSalas lo fija en ConfiguracionPartida.CantidadNiveles
const NIVELES_POR_DEFECTO = 5;

// ReglasJuego.DuracionTransicion
const SEGUNDOS_TRANSICION = 5;

const TECLAS: Record<string, Direccion> = {
    ArrowUp: Direccion.Arriba,
    KeyW: Direccion.Arriba,
    ArrowDown: Direccion.Abajo,
    KeyS: Direccion.Abajo,
    ArrowLeft: Direccion.Izquierda,
    KeyA: Direccion.Izquierda,
    ArrowRight: Direccion.Derecha,
    KeyD: Direccion.Derecha,
};

// Puntos que se muestran flotando (ReglasJuego)
const PUNTOS_EVENTO: Partial<Record<TipoEventoJuego, string>> = {
    [TipoEventoJuego.FantasmaComido]: '200',
    [TipoEventoJuego.PacManAtrapado]: '300',
};

interface DatosEscenaJuego {
    codigoSala: string;
}

// La partida: dibuja lo que manda el servidor y le envía las intenciones del
// jugador. Nunca mueve a nadie por su cuenta; el servidor es la autoridad.
export class EscenaJuego extends Scene {
    private estado: EstadoCliente;
    private laberinto: VistaLaberinto | null = null;
    private vistas = new Map<number, VistaActor>();
    private hud: HUD;
    private sonido: GestorSonido;
    private objetos = new Map<number, TObjeto>();
    private aviso: GameObjects.Container | null = null;
    private cuentaRegresiva: GameObjects.Text | null = null;
    private finTransicion = 0;
    private ultimaDireccion: Direccion = Direccion.Ninguna;
    // Nivel y estado de la sala del último cartel de fin mostrado
    private finMostrado = '';
    private distribucion = calcularDistribucionJuego(1366, 768);
    private tamanoPantalla = '';
    private readonly zonaMapa = new Geom.Rectangle();
    private tactiles: ControlesTactiles | null = null;
    private cruceta: GameObjects.Container | null = null;

    constructor() {
        super('EscenaJuego');
    }

    init(datos: DatosEscenaJuego): void {
        this.estado = new EstadoCliente(datos.codigoSala, obtenerSesion()?.usuario.idUsuario ?? 0);
        this.laberinto = null;
        this.vistas = new Map();
        this.aviso = null;
        this.cuentaRegresiva = null;
        this.ultimaDireccion = Direccion.Ninguna;
        this.finMostrado = '';
        this.tamanoPantalla = '';
        this.tactiles = null;
        this.cruceta = null;
    }

    create(): void {
        prepararPantalla(this, 'juego');
        this.cameras.main.setBackgroundColor(COLOR.FONDO);

        this.sonido = new GestorSonido(this);
        this.ajustarDistribucion();
        this.crearControles();
        this.scale.on('resize', this.ajustarDistribucion, this);
        this.events.once('shutdown', () => this.scale.off('resize', this.ajustarDistribucion, this));
        this.suscribirse();
        this.cargarInicial();
        this.cargarObjetos();
    }

    update(): void {
        const ahora = performance.now() / 1000;
        const muestra = this.estado.interpolacion.muestrear(ahora);

        if (this.cuentaRegresiva) {
            const restante = Math.max(0, Math.ceil(this.finTransicion - ahora));
            this.cuentaRegresiva.setText(`SIGUIENTE NIVEL EN ${restante}`);
        }

        if (!muestra || !this.laberinto) {
            return;
        }

        // Las bolas y cajas se quitan al dibujar ese instante, no al recibirlo,
        // para que desaparezcan justo cuando Pac-Man pasa
        this.laberinto.actualizarPuntos(muestra.siguiente.puntosConsumidos);
        this.laberinto.actualizarCajas(muestra.siguiente.cajas);

        const mio = this.estado.miActor(muestra.siguiente);
        this.laberinto.actualizarTrampas(muestra.siguiente.trampas, mio?.bando === Bando.Fantasma);

        for (const { actor, bando } of this.estado.actores(muestra.siguiente)) {
            const anterior = this.estado.buscarActor(muestra.anterior, actor.idPersonaje) ?? actor;
            const posicion = interpolarPosicion(anterior, actor, muestra.fraccion);
            const { x, y } = this.laberinto.centro(posicion.fila, posicion.columna);

            this.obtenerVista(actor.idPersonaje, bando, actor.idPersonaje === mio?.actor.idPersonaje)
                .actualizar(actor, x, y, ahora, estadoVisual(actor, bando, muestra.siguiente.efectos));
        }

        const ultimo = this.estado.estado;
        if (ultimo) {
            this.hud.actualizarEfectos(ultimo, muestra.tiempoSimulacion, this.objetos);
        }
    }

    // ---------- Datos del servidor ----------

    private suscribirse(): void {
        const cancelar = [
            conexionTiempoReal.on('SalaActualizada', (sala) => this.alActualizarSala(sala)),
            conexionTiempoReal.on('NivelIniciado', (nivel) => this.alIniciarNivel(nivel)),
            conexionTiempoReal.on('EstadoActualizado', (estado) => this.alRecibirEstado(estado)),
            conexionTiempoReal.alCambiarEstado((estado) => {
                if (estado === 'RECONECTANDO') {
                    this.mostrarAviso('RECONECTANDO...', 'La IA mueve tu personaje mientras tanto');
                } else if (estado === 'CONECTADO' && this.estado.sala?.estado === EstadoSala.Jugando) {
                    this.quitarAviso();
                }
            }),
        ];

        this.events.once('shutdown', () => {
            cancelar.forEach((cancelarUno) => cancelarUno());
            this.sonido.destruir();
            this.laberinto?.destruir();
        });
    }

    // El primer NivelIniciado pudo llegar mientras el lobby cambiaba de escena
    private async cargarInicial(): Promise<void> {
        const codigo = this.estado.codigoSala;
        const [sala, nivel, estado] = await Promise.all([
            conexionTiempoReal.obtenerSala(codigo),
            conexionTiempoReal.obtenerNivel(codigo),
            conexionTiempoReal.obtenerEstado(codigo),
        ]);

        if (!this.sys.isActive()) {
            return;
        }

        if (sala.data) this.alActualizarSala(sala.data);
        if (nivel.data) this.alIniciarNivel(nivel.data);
        if (estado.data) this.alRecibirEstado(estado.data);
    }

    private async cargarObjetos(): Promise<void> {
        const respuesta = await listarObjetos();

        for (const objeto of respuesta.data ?? []) {
            this.objetos.set(objeto.idObjeto, objeto);
        }
    }

    private alActualizarSala(sala: SalaDto): void {
        if (sala.codigo !== this.estado.codigoSala) {
            return;
        }

        const anterior = this.estado.sala?.estado;
        this.estado.sala = sala;

        // Ya no tiene plaza en la sala (salió desde otro dispositivo, por ejemplo)
        if (!sala.jugadores.some((jugador) => jugador.idUsuario === this.estado.idUsuario)) {
            this.scene.start('EscenaMenu');
            return;
        }

        if (sala.estado === EstadoSala.Transicion && anterior !== EstadoSala.Transicion) {
            this.sonido.intermision();
            this.finTransicion = performance.now() / 1000 + SEGUNDOS_TRANSICION;
        }

        this.mostrarFin();
    }

    private alIniciarNivel(nivel: NivelDto): void {
        if (nivel.codigoSala !== this.estado.codigoSala) {
            return;
        }

        // Al reconectar el servidor vuelve a mandar el mismo nivel
        if (this.estado.nivel?.numeroNivel === nivel.numeroNivel && this.laberinto) {
            return;
        }

        this.estado.nivel = nivel;
        this.estado.interpolacion.reiniciar();
        this.sonido.detenerMusica();
        this.quitarAviso();

        this.dibujarMapa(nivel);

        // El servidor recuerda la última dirección; en un nivel nuevo empieza sin ninguna
        this.ultimaDireccion = Direccion.Ninguna;
    }

    private alRecibirEstado(estado: EstadoPartidaDto): void {
        if (estado.codigoSala !== this.estado.codigoSala) {
            return;
        }

        const primero = this.estado.estado === null;
        this.estado.interpolacion.agregar(estado, performance.now() / 1000);

        if (!primero) {
            this.sonido.reproducirEventos(estado.eventos);
            this.mostrarEventos(estado.eventos);
        } else {
            this.mostrarPresentacion(estado);
        }

        this.hud.actualizar(
            estado,
            this.estado.actores(estado),
            this.estado.miActor(estado),
            this.estado.sala?.cantidadNiveles ?? NIVELES_POR_DEFECTO,
            this.objetos,
        );

        this.mostrarFin();
    }

    // ---------- Controles ----------

    private crearControles(): void {
        this.tactiles = new ControlesTactiles(this, this.zonaMapa, (direccion) => this.enviarDireccion(direccion));
        this.ajustarCruceta();

        this.input.keyboard?.on('keydown', (evento: KeyboardEvent) => {
            const direccion = TECLAS[evento.code];

            if (direccion !== undefined) {
                this.enviarDireccion(direccion);
            } else if (evento.code === 'Space') {
                this.usarObjeto();
            } else if (evento.code === 'KeyM') {
                this.hud.alternarSilencio();
            }
        });
    }

    // El servidor guarda la última dirección y gira en cuanto puede, así que
    // basta con avisar cuando cambia
    private enviarDireccion(direccion: Direccion): void {
        if (direccion === this.ultimaDireccion) {
            return;
        }

        this.ultimaDireccion = direccion;
        conexionTiempoReal.enviarDireccion(this.estado.codigoSala, direccion).then((respuesta) => {
            // Rechazada (entre niveles, por ejemplo): se permite reintentar
            if (!respuesta.success && this.ultimaDireccion === direccion) {
                this.ultimaDireccion = Direccion.Ninguna;
            }
        });
    }

    private usarObjeto(): void {
        conexionTiempoReal.usarObjeto(this.estado.codigoSala);
    }

    private async salir(): Promise<void> {
        // La IA toma el personaje hasta el final del nivel
        await conexionTiempoReal.salirSala(this.estado.codigoSala);

        if (this.sys.isActive()) {
            this.scene.start('EscenaMenu');
        }
    }

    // ---------- Dibujo ----------

    private ajustarDistribucion(): void {
        const tamano = `${this.scale.width}:${this.scale.height}:${this.scale.displaySize.width}`;

        if (tamano === this.tamanoPantalla) {
            return;
        }

        this.tamanoPantalla = tamano;
        const anterior = this.distribucion;
        const escala = this.scale.displaySize.width / this.scale.width;
        this.distribucion = calcularDistribucionJuego(this.scale.width, this.scale.height, escala);
        const { izquierda, derecha, mapa } = this.distribucion;
        this.cameras.main.setViewport(0, 0, this.scale.width, this.scale.height).setScroll(0, 0);
        this.zonaMapa.setTo(mapa.x, mapa.y, mapa.ancho, mapa.alto);
        this.hud?.destruir();
        this.hud = new HUD(this, {
            izquierda,
            derecha,
            alUsarObjeto: () => this.usarObjeto(),
            alSalir: () => this.salir(),
        });
        this.ajustarCruceta();

        if (this.estado.nivel) {
            this.dibujarMapa(this.estado.nivel);
        }

        if (this.aviso) {
            this.aviso.x = mapa.x + mapa.ancho / 2;
            this.aviso.y += mapa.y - anterior.mapa.y;
        }

        const estado = this.estado.estado;

        if (estado) {
            this.hud.actualizar(
                estado,
                this.estado.actores(estado),
                this.estado.miActor(estado),
                this.estado.sala?.cantidadNiveles ?? NIVELES_POR_DEFECTO,
                this.objetos,
            );
        }
    }

    private ajustarCruceta(): void {
        this.cruceta?.destroy();
        this.cruceta = null;

        if (!this.tactiles || !this.sys.game.device.input.touch) {
            return;
        }

        const zona = this.distribucion.izquierda;
        const escala = this.scale.displaySize.width / this.scale.width;
        const lado = Math.min((zona.ancho - 36) / 3, Math.max(64, 44 / escala));
        this.cruceta = this.tactiles.crearCruceta(
            zona.x + zona.ancho / 2,
            zona.y + zona.alto - (lado * 1.5 + 6) - 16,
            lado
        );
    }

    private dibujarMapa(nivel: NivelDto): void {
        this.vistas.forEach(vista => vista.destruir());
        this.vistas.clear();
        this.laberinto?.destruir();
        const zona = this.distribucion.mapa;
        const tamano = Math.floor(Math.min(zona.ancho / nivel.columnas, zona.alto / nivel.filas));
        const x = zona.x + (zona.ancho - nivel.columnas * tamano) / 2;
        const y = zona.y + (zona.alto - nivel.filas * tamano) / 2;
        this.laberinto = new VistaLaberinto(this, nivel, x, y, tamano);
        this.laberinto.actualizarPuntos(this.estado.estado?.puntosConsumidos ?? []);
    }

    private obtenerVista(idPersonaje: number, bando: Bando, esPropio: boolean): VistaActor {
        let vista = this.vistas.get(idPersonaje);

        if (!vista) {
            const escala = (this.laberinto?.tamano ?? CASILLA_BASE) / CASILLA_BASE;
            vista = new VistaActor(this, idPersonaje, bando === Bando.PacMan, esPropio, escala);
            this.vistas.set(idPersonaje, vista);
        }

        return vista;
    }

    // Puntos que flotan donde se comió a alguien, y un destello si chocan fantasmas
    private mostrarEventos(eventos: EventoDto[]): void {
        if (!this.laberinto) {
            return;
        }

        for (const evento of eventos) {
            if (evento.fila === null || evento.columna === null) {
                continue;
            }

            const { x, y } = this.laberinto.centro(evento.fila, evento.columna);
            const puntos = PUNTOS_EVENTO[evento.tipo];

            if (puntos) {
                const texto = this.add.text(x, y, puntos, estiloTexto(10, COLOR.CELESTE)).setOrigin(0.5).setDepth(5);
                this.tweens.add({ targets: texto, y: y - 24, alpha: 0, duration: 900, onComplete: () => texto.destroy() });
            } else if (evento.tipo === TipoEventoJuego.Explosion) {
                this.destello(x, y, COLOR.ROSA, 4);
            } else if (evento.tipo === TipoEventoJuego.TrampaActivada) {
                this.destello(x, y, COLOR.HIELO, 5);
            } else if (evento.tipo === TipoEventoJuego.ObjetoUsado) {
                // El teletransporte avisa donde aparece (el evento trae la casilla de llegada)
                const teletransporte = this.objetos.get(evento.idObjeto ?? 0)?.efecto === EFECTO.TELETRANSPORTE;
                this.destello(x, y, teletransporte ? COLOR.LENTO : COLOR.BLANCO, teletransporte ? 6 : 3);
            }
        }
    }

    private destello(x: number, y: number, color: number, escala: number): void {
        const circulo = this.add.circle(x, y, 6, color).setDepth(5);
        this.tweens.add({ targets: circulo, scale: escala, alpha: 0, duration: 400, onComplete: () => circulo.destroy() });
    }

    // Al empezar cada nivel: qué nivel es y qué rol te tocó. Es un cartel
    // chico arriba del mapa porque el nivel ya está corriendo.
    private mostrarPresentacion(estado: EstadoPartidaDto): void {
        const mio = this.estado.miActor(estado);
        const rol = mio?.bando === Bando.Fantasma ? 'ERES FANTASMA' : 'ERES PAC-MAN';
        const total = this.estado.sala?.cantidadNiveles ?? NIVELES_POR_DEFECTO;

        this.mostrarAviso(`NIVEL ${estado.numeroNivel}/${total}`, rol, [], 0, this.distribucion.mapa.y + 80);

        const aviso = this.aviso;
        this.tweens.add({
            targets: aviso,
            alpha: 0,
            delay: 1500,
            duration: 400,
            onComplete: () => {
                if (this.aviso === aviso) {
                    this.quitarAviso();
                }
            },
        });
    }

    // El estado final del nivel y el cambio de la sala (Transicion o Finalizada)
    // llegan por separado y en cualquier orden: el cartel se arma con ambos
    private mostrarFin(): void {
        const estado = this.estado.estado;
        const salaEstado = this.estado.sala?.estado;

        if (!estado?.terminada) {
            return;
        }

        const clave = `${estado.numeroNivel}-${salaEstado}`;
        if (clave === this.finMostrado) {
            return;
        }

        this.finMostrado = clave;

        if (salaEstado === EstadoSala.Finalizada) {
            this.mostrarFinDePartida();
        } else {
            this.mostrarFinDeNivel(salaEstado === EstadoSala.Transicion);
        }
    }

    private mostrarFinDeNivel(conCuenta: boolean, titulo?: string): number {
        const estado = this.estado.estado;

        if (!estado) {
            return 0;
        }

        const ganador = estado.bandoGanador === Bando.PacMan ? 'GANAN LOS PAC-MAN'
            : estado.bandoGanador === Bando.Fantasma ? 'GANAN LOS FANTASMAS' : 'NIVEL TERMINADO';

        // Tabla de texto con columnas fijas (la fuente es monoespaciada).
        // Más adelante EscenaResultados puede mostrar el detalle completo.
        const puntajes = this.estado.actores(estado)
            .map(({ actor }) => actor)
            .sort((a, b) => b.puntuacion - a.puntuacion)
            .map((actor) => {
                const nombre = actor.idUsuario === this.estado.idUsuario ? 'TU' : actor.nombreUsuario ?? 'IA';
                return `${nombre.slice(0, 12).padEnd(12, ' ')} ${String(actor.puntuacion).padStart(6, ' ')}`;
            });

        const pieY = this.mostrarAviso(titulo ?? `NIVEL ${estado.numeroNivel} TERMINADO`, ganador, puntajes, 56);

        if (conCuenta && this.aviso) {
            this.cuentaRegresiva = this.add.text(0, pieY, '', estiloTexto(11, COLOR.GRIS_CLARO)).setOrigin(0.5);
            this.aviso.add(this.cuentaRegresiva);
        }

        return pieY;
    }

    private mostrarFinDePartida(): void {
        const pieY = this.mostrarFinDeNivel(false, 'PARTIDA TERMINADA');

        // La sala queda finalizada hasta que todos salen; salir libera la plaza
        this.aviso?.add(new Boton(this, 0, pieY, 'VOLVER AL MENU', {
            ancho: 300,
            alto: 48,
            tamanoTexto: 12,
            color: COLOR.PACMAN,
            alPulsar: () => this.salir(),
        }));
    }

    // Cartel sobre el mapa. pie: espacio libre abajo para un botón o una cuenta;
    // devuelve dónde va (relativo al centro del cartel).
    private mostrarAviso(titulo: string, subtitulo: string, lineas: string[] = [], pie = 0, centroY?: number): number {
        this.quitarAviso();

        const alto = 112 + (lineas.length > 0 ? 16 + lineas.length * 26 : 0) + pie;
        const arriba = -alto / 2;
        const aviso = this.add
            .container(
                this.distribucion.mapa.x + this.distribucion.mapa.ancho / 2,
                centroY ?? this.distribucion.mapa.y + this.distribucion.mapa.alto / 2
            )
            .setDepth(10);

        aviso.add([
            new Recuadro(this, 0, 0, 520, alto, 12, true).dibujar(COLOR.PANEL, COLOR.MURO_CLARO, 3),
            this.add.text(0, arriba + 40, titulo, estiloTexto(20, COLOR.PACMAN)).setOrigin(0.5),
            this.add.text(0, arriba + 76, subtitulo, estiloTexto(12, COLOR.BLANCO)).setOrigin(0.5),
            ...lineas.map((linea, i) =>
                this.add.text(0, arriba + 124 + i * 26, linea, estiloTexto(11, COLOR.GRIS_CLARO)).setOrigin(0.5)),
        ]);

        this.aviso = aviso;

        return alto / 2 - pie / 2 - 4;
    }

    private quitarAviso(): void {
        this.aviso?.destroy();
        this.aviso = null;
        this.cuentaRegresiva = null;
    }
}
