import { ANCHO_INTERFAZ } from '../ui/PantallaAdaptable';
import { GameObjects, Scene } from 'phaser';
import { Bando, CodigoRol, EstadoSala, FrecuenciaCajas } from '../../modelos/tiempo-real/enums';
import { ConfiguracionSalaDto, JugadorSalaDto, SalaDto } from '../../modelos/tiempo-real/sala.dto';
import { listarDificultades, obtenerSesion } from '../../servicios/ClienteApi';
import { conexionTiempoReal } from '../../servicios/ConexionTiempoReal';
import { SONIDO } from '../sonidos/CatalogoSonidos';
import { Boton } from '../ui/Boton';
import { COLOR, css, estiloTexto } from '../ui/Paleta';
import { Recuadro, crearPanel, dibujarMarco } from '../ui/Panel';
import { OpcionSelector, Selector } from '../ui/Selector';

// Escena que recibe la partida al empezar. La hace otra parte del equipo:
// mientras no esté registrada, la sala solo avisa que la partida empezó.
const ESCENA_JUEGO = 'EscenaJuego';

// Coinciden con ConfiguracionSala (VidasMinimas, VidasMaximas)
const VIDAS_MINIMAS = 1;
const VIDAS_MAXIMAS = 5;

// Por si api/Dificultad/Listar falla; mismos datos que 06_DatosCatalogos.sql
const DIFICULTADES_POR_DEFECTO = [
    { idDificultad: 1, nombre: 'FACIL' },
    { idDificultad: 2, nombre: 'NORMAL' },
    { idDificultad: 3, nombre: 'DIFICIL' },
];

const OPCIONES_CAJAS: OpcionSelector<FrecuenciaCajas>[] = [
    { valor: FrecuenciaCajas.Ninguna, texto: 'NINGUNA' },
    { valor: FrecuenciaCajas.Baja, texto: 'POCAS' },
    { valor: FrecuenciaCajas.Normal, texto: 'NORMAL' },
    { valor: FrecuenciaCajas.Alta, texto: 'MUCHAS' },
];

const OPCIONES_ACTIVADO: OpcionSelector<boolean>[] = [
    { valor: true, texto: 'ACTIVADO' },
    { valor: false, texto: 'DESACTIVADO' },
];

const OPCIONES_MODO: OpcionSelector<boolean>[] = [
    { valor: false, texto: 'NORMAL' },
    { valor: true, texto: 'CLASICO' },
];

// Coinciden con ConfiguracionSala.OpcionesReaparicionFantasmas
const OPCIONES_REAPARICION: OpcionSelector<number>[] = [30, 20, 10, 5]
    .map((segundos) => ({ valor: segundos, texto: `${segundos} SEG` }));

const OPCIONES_ROL: OpcionSelector<CodigoRol>[] = [
    { valor: null, texto: 'AL AZAR' },
    { valor: 'PACMAN', texto: 'PAC-MAN' },
    { valor: 'FANTASMA', texto: 'FANTASMA' },
];

// Mismos colores que las plazas de la lista de salas
const COLORES_PLAZA = [COLOR.PACMAN, COLOR.VERDE, COLOR.ROJO, COLOR.ROSA];

const PANEL_Y = 150;
const PANEL_ALTO = 440;
const PLAZA_ALTO = 84;

// Filas del menú de configuración; se acomodan según cuáles se ven
const PRIMERA_OPCION_Y = PANEL_Y + 80;
const ALTO_OPCION = 46;

interface Plaza {
    color: number;
    fondo: Recuadro;
    insignia: Recuadro;
    numero: GameObjects.Text;
    nombre: GameObjects.Text;
    etiquetas: GameObjects.Text;
    esperando: GameObjects.Text;
    expulsar: Boton;
    idJugador: number | null;
}

interface DatosEscenaSala {
    sala: SalaDto;
}

// Lobby: jugadores de la sala, menú de configuración del anfitrión e inicio.
export class EscenaSala extends Scene {
    private sala: SalaDto;
    private idUsuario: number;

    private titulo: GameObjects.Text;
    private modo: GameObjects.Text;
    private cabeceraJugadores: GameObjects.Text;
    private plazas: Plaza[] = [];
    private notaConfiguracion: GameObjects.Text;
    private mensaje: GameObjects.Text;
    private espera: GameObjects.Text;
    private botonIniciar: Boton;
    private botonBloquear: Boton;


    private selectorDificultad: Selector<number> | null = null;
    private selectorVidas: Selector<number> | null = null;
    private selectorChoque: Selector<boolean> | null = null;
    private selectorCajas: Selector<FrecuenciaCajas> | null = null;
    private selectorBandos: Selector<boolean> | null = null;
    private selectorModo: Selector<boolean> | null = null;
    private selectorReaparicion: Selector<number> | null = null;
    private selectorRol: Selector<CodigoRol> | null = null;
    private selectorX: number;
    private selectorAncho: number;

    constructor() {
        super('EscenaSala');
    }

    init(datos: DatosEscenaSala): void {
        this.sala = datos.sala;
        this.idUsuario = obtenerSesion()?.usuario.idUsuario ?? 0;
        this.plazas = [];
        this.selectorDificultad = null;
    }

    create(): void {
        const width = ANCHO_INTERFAZ;

        dibujarMarco(this);

        this.titulo = this.add.text(width / 2, 60, '', estiloTexto(28, COLOR.PACMAN)).setOrigin(0.5);
        this.modo = this.add.text(width / 2, 106, '', estiloTexto(11, COLOR.GRIS_CLARO)).setOrigin(0.5);

        this.crearPanelJugadores(40, width / 2 - 52);
        this.crearPanelConfiguracion(width / 2 + 12, width / 2 - 52);

        this.mensaje = this.add.text(width / 2, 622, '', estiloTexto(12, COLOR.ROSA)).setOrigin(0.5);

        new Boton(this, 230, 680, 'SALIR', {
            ancho: 220,
            color: COLOR.ROJO,
            alPulsar: () => this.salir(),
        });

        this.botonIniciar = new Boton(this, width - 250, 680, 'INICIAR PARTIDA', {
            ancho: 360,
            color: COLOR.PACMAN,
            alPulsar: () => this.iniciar(),
        });

                this.botonBloquear = new Boton(this, 467, 680, 'BLOQUEAR', {
            ancho: 200,
            tamanoTexto: 12,
            color: COLOR.NARANJA,
            alPulsar: () => this.bloquear(),
        });

        this.espera = this.add
            .text(width - 250, 680, 'ESPERANDO AL ANFITRION', estiloTexto(12, COLOR.GRIS_CLARO))
            .setOrigin(0.5);
        this.tweens.add({ targets: this.espera, alpha: 0.3, duration: 700, yoyo: true, repeat: -1 });

        const cancelar = conexionTiempoReal.on('SalaActualizada', (sala) => {
            if (sala.codigo === this.sala.codigo) {
                this.actualizar(sala);
            }
        });
        const cancelarEstado = conexionTiempoReal.alCambiarEstado((estado) => {
            this.mostrarMensaje(estado === 'RECONECTANDO' ? 'Reconectando...' : '');
        });
        const cancelarExpulsion = conexionTiempoReal.on('Expulsado', (codigo) => {
            if (codigo === this.sala.codigo) {
                this.scene.start('EscenaListaSalas', { aviso: 'El anfitrión te expulsó de la sala' });
            }
        });


        this.events.once('shutdown', () => {
            cancelar();
            cancelarEstado();
            cancelarExpulsion();
        });

        // Volvió a una sala con la partida en curso (reconexión): directo al juego
        if (this.sala.estado === EstadoSala.Jugando || this.sala.estado === EstadoSala.Transicion) {
            this.irAlJuego();
            return;
        }

        this.mostrar(this.sala);
        this.cargarDificultades();
    }

    // ---------- Acciones ----------

    private actualizar(sala: SalaDto): void {
        const anterior = this.sala;
        this.sala = sala;

        if (!sala.jugadores.some((jugador) => jugador.idUsuario === this.idUsuario)) {
            this.scene.start('EscenaListaSalas');

            return;
        }

        // Llega alguien nuevo: el sonido de vida extra del arcade
        if (sala.jugadores.length > anterior.jugadores.length) {
            this.sound.play(SONIDO.VIDA, { volume: 0.5 });
        }

        if (sala.estado === EstadoSala.Jugando) {
            this.irAlJuego();

            return;
        }

        this.mostrar(sala);
    }

    private async configurar(cambios: Partial<ConfiguracionSalaDto>): Promise<void> {
        const respuesta = await conexionTiempoReal.configurarSala(
            this.sala.codigo, { ...this.sala.configuracion, ...cambios });

        this.tratarRespuesta(respuesta);
    }

    private async elegirRol(rol: CodigoRol): Promise<void> {
        this.tratarRespuesta(await conexionTiempoReal.elegirRol(this.sala.codigo, rol));
    }

    private async iniciar(): Promise<void> {
        this.botonIniciar.setHabilitado(false);
        this.tratarRespuesta(await conexionTiempoReal.iniciarPartida(this.sala.codigo));
    }

    private async salir(): Promise<void> {
        await conexionTiempoReal.salirSala(this.sala.codigo);

        // Aunque falle (la sala ya no existe) no tiene sentido quedarse aquí
        if (this.sys.isActive()) {
            this.scene.start('EscenaListaSalas');
        }
    }

        private async bloquear(): Promise<void> {
        this.botonBloquear.setHabilitado(false);
        const respuesta = await conexionTiempoReal.bloquearSala(this.sala.codigo, !this.sala.bloqueada);

        if (this.sys.isActive()) {
            this.botonBloquear.setHabilitado(true);
        }

        this.tratarRespuesta(respuesta);
    }

    private async expulsar(idJugador: number | null): Promise<void> {
        if (idJugador === null) {
            return;
        }

        this.tratarRespuesta(await conexionTiempoReal.expulsarJugador(this.sala.codigo, idJugador));
    }

    // El aviso SalaActualizada trae el cambio; aquí solo se trata el rechazo
    private tratarRespuesta(respuesta: { success: boolean; data: SalaDto | null; error: string }): void {
        if (!this.sys.isActive()) {
            return;
        }

        if (respuesta.success && respuesta.data) {
            this.mostrarMensaje('');
            this.actualizar(respuesta.data);
        } else {
            this.mostrarMensaje(respuesta.error);
            // Devuelve los selectores al valor real
            this.mostrar(this.sala);
        }
    }

    private irAlJuego(): void {
        if (this.scene.manager.keys[ESCENA_JUEGO]) {
            this.scene.start(ESCENA_JUEGO, { codigoSala: this.sala.codigo });
        } else {
            this.mostrar(this.sala);
            this.mostrarMensaje('¡La partida empezó! (EscenaJuego aún no existe)');
        }
    }

    private async cargarDificultades(): Promise<void> {
        const respuesta = await listarDificultades();
        const dificultades = respuesta.success && respuesta.data?.length
            ? respuesta.data
            : DIFICULTADES_POR_DEFECTO;

        if (!this.sys.isActive()) {
            return;
        }

        const opciones = dificultades.map((dificultad) => ({
            valor: dificultad.idDificultad,
            texto: dificultad.nombre.toUpperCase(),
        }));

        this.selectorDificultad = new Selector(
            this, this.selectorX, PRIMERA_OPCION_Y, this.selectorAncho, 'DIFICULTAD', opciones,
            (idDificultad) => this.configurar({ idDificultad }));

        this.mostrar(this.sala);
    }

    // ---------- Dibujo ----------

    private crearPanelJugadores(x: number, ancho: number): void {
        crearPanel(this, x, PANEL_Y, ancho, PANEL_ALTO);
        this.cabeceraJugadores = this.add.text(x + 24, PANEL_Y + 28, '', estiloTexto(14, COLOR.MURO_CLARO));

        for (let i = 0; i < 4; i++) {
            const y = PANEL_Y + 60 + i * (PLAZA_ALTO + 6);
            const color = COLORES_PLAZA[i];
            const centro = y + PLAZA_ALTO / 2;

            const fondo = new Recuadro(this, x + 16, y, ancho - 32, PLAZA_ALTO, 8);
            const insignia = new Recuadro(this, x + 56, centro, 40, 40, 6, true);
            const numero = this.add.text(x + 56, centro, String(i + 1), estiloTexto(16, COLOR.FONDO)).setOrigin(0.5);
            const nombre = this.add.text(x + 96, centro - 12, '', estiloTexto(14, COLOR.BLANCO)).setOrigin(0, 0.5);
            const etiquetas = this.add.text(x + 96, centro + 16, '', estiloTexto(10, COLOR.PACMAN)).setOrigin(0, 0.5);
            const esperando = this.add
                .text(x + 96, centro, 'ESPERANDO...', estiloTexto(12, COLOR.GRIS))
                .setOrigin(0, 0.5);

            this.tweens.add({ targets: esperando, alpha: 0.35, duration: 800, yoyo: true, repeat: -1, delay: i * 200 });
            const expulsar = new Boton(this, x + ancho - 60, centro, 'X', {
                ancho: 48,
                alto: 48,
                tamanoTexto: 14,
                color: COLOR.ROJO,
                alPulsar: () => this.expulsar(this.plazas[i].idJugador),
            });

            this.plazas.push({ color, fondo, insignia, numero, nombre, etiquetas, esperando, expulsar, idJugador: null });
        }
    }

    private crearPanelConfiguracion(x: number, ancho: number): void {
        crearPanel(this, x, PANEL_Y, ancho, PANEL_ALTO);
        this.add.text(x + 24, PANEL_Y + 28, 'CONFIGURACION', estiloTexto(14, COLOR.MURO_CLARO));

        const selectorX = x + 24;
        const selectorAncho = ancho - 48;
        this.selectorX = selectorX;
        this.selectorAncho = selectorAncho;

        // DIFICULTAD va primero y se crea al recibir el catálogo. Las filas
        // se crean en y = 0 y mostrar() las acomoda.
        const vidas: OpcionSelector<number>[] = [];
        for (let v = VIDAS_MINIMAS; v <= VIDAS_MAXIMAS; v++) {
            vidas.push({ valor: v, texto: String(v) });
        }

        this.selectorVidas = new Selector(this, selectorX, 0, selectorAncho, 'VIDAS', vidas,
            (cantidad) => this.configurar({ vidas: cantidad }));

        this.selectorChoque = new Selector(this, selectorX, 0, selectorAncho, 'CHOQUE PAC-MAN',
            OPCIONES_ACTIVADO, (colisionEntrePacMan) => this.configurar({ colisionEntrePacMan }));

        this.selectorCajas = new Selector(this, selectorX, 0, selectorAncho, 'CAJAS', OPCIONES_CAJAS,
            (frecuenciaCajas) => this.configurar({ frecuenciaCajas }));

        // Solo en multijugador: contra la IA el rol ya se conserva
        this.selectorBandos = new Selector(this, selectorX, 0, selectorAncho, 'BANDOS FIJOS',
            OPCIONES_ACTIVADO, (bandosFijos) => this.configurar({ bandosFijos }));

        this.selectorModo = new Selector(this, selectorX, 0, selectorAncho, 'MODO', OPCIONES_MODO,
            (modoClasico) => this.configurar({ modoClasico }));

        // Solo en el modo clásico
        this.selectorReaparicion = new Selector(this, selectorX, 0, selectorAncho, 'REAPARICION',
            OPCIONES_REAPARICION,
            (segundosReaparicionFantasmas) => this.configurar({ segundosReaparicionFantasmas }));

        // Solo aparece si juega solo contra la IA
        this.selectorRol = new Selector(this, selectorX, 0, selectorAncho, 'TU ROL', OPCIONES_ROL,
            (rol) => this.elegirRol(rol));

        this.notaConfiguracion = this.add
            .text(x + ancho / 2, PANEL_Y + PANEL_ALTO - 36, '', estiloTexto(10, COLOR.GRIS))
            .setOrigin(0.5);
    }

    private mostrar(sala: SalaDto): void {
        const soyAnfitrion = sala.idAnfitrion === this.idUsuario;
        const enLobby = sala.estado === EstadoSala.Lobby;
        const editable = soyAnfitrion && enLobby;
        const solo = sala.jugadores.length === 1;

        this.titulo.setText(`SALA ${sala.codigo}`);
        const configuracion = sala.configuracion;
         const descripcion = solo
            ? 'TU CONTRA LA IA · ELIGE TU ROL'
            : configuracion.bandosFijos
                ? 'MULTIJUGADOR · ROLES AL AZAR SOLO AL EMPEZAR'
                : 'MULTIJUGADOR · ROLES AL AZAR EN CADA NIVEL';
        this.modo.setText(sala.bloqueada ? `${descripcion} · BLOQUEADA` : descripcion); 
        this.cabeceraJugadores.setText(`JUGADORES ${sala.jugadores.length}/${sala.capacidadHumanos}`);

        this.plazas.forEach((plaza, i) => this.mostrarPlaza(plaza, sala.jugadores[i], editable));

        this.selectorDificultad?.setValor(configuracion.idDificultad).setEditable(editable);
        this.selectorVidas?.setValor(configuracion.vidas).setEditable(editable);
        this.selectorChoque?.setValor(configuracion.colisionEntrePacMan).setEditable(editable);
        this.selectorCajas?.setValor(configuracion.frecuenciaCajas).setEditable(editable);
        this.selectorBandos?.setValor(configuracion.bandosFijos).setEditable(editable);
        this.selectorModo?.setValor(configuracion.modoClasico).setEditable(editable);
        this.selectorReaparicion
            ?.setValor(configuracion.segundosReaparicionFantasmas)
            .setEditable(editable);

        const yo = sala.jugadores.find((jugador) => jugador.idUsuario === this.idUsuario);
        this.selectorRol
            ?.setVisible(solo)
            .setValor(this.codigoRol(yo?.rolPreferido ?? null))
            .setEditable(enLobby);

        this.acomodarOpciones(solo, configuracion.modoClasico);

        this.notaConfiguracion.setText(soyAnfitrion ? '' : 'SOLO EL ANFITRION LA CAMBIA');

        this.botonIniciar.setVisible(soyAnfitrion).setHabilitado(sala.puedeIniciar);
        this.botonBloquear.setVisible(editable).setTexto(sala.bloqueada ? 'DESBLOQUEAR' : 'BLOQUEAR');
        this.espera.setVisible(!soyAnfitrion && enLobby);
    }

    // Una fila por opción visible, sin huecos de las que no aplican
    private acomodarOpciones(solo: boolean, modoClasico: boolean): void {
        const filas: [GameObjects.Container | null, boolean][] = [
            [this.selectorDificultad, true],
            [this.selectorVidas, true],
            [this.selectorChoque, true],
            [this.selectorCajas, true],
            [this.selectorBandos, !solo],
            [this.selectorModo, true],
            [this.selectorReaparicion, modoClasico],
            [this.selectorRol, solo],
        ];
        let y = PRIMERA_OPCION_Y;

        for (const [selector, visible] of filas) {
            selector?.setVisible(visible).setY(y);

            if (visible) {
                y += ALTO_OPCION;
            }
        }
    }

        private mostrarPlaza(plaza: Plaza, jugador: JugadorSalaDto | undefined, puedeExpulsar: boolean): void {
        const ocupada = jugador !== undefined;

        plaza.idJugador = jugador?.idUsuario ?? null;
        plaza.expulsar.setVisible(puedeExpulsar && ocupada && jugador.idUsuario !== this.idUsuario);
        plaza.insignia.dibujar(ocupada ? plaza.color : COLOR.PANEL, plaza.color, 2);
        plaza.insignia.setAlpha(ocupada ? 1 : 0.4);
        plaza.numero.setAlpha(ocupada ? 1 : 0.4);
        plaza.nombre.setVisible(ocupada);
        plaza.etiquetas.setVisible(ocupada);
        plaza.esperando.setVisible(!ocupada);

        if (!jugador) {
            plaza.fondo.dibujar(COLOR.FONDO);

            return;
        }

        const esYo = jugador.idUsuario === this.idUsuario;
        const etiquetas: string[] = [];

        if (jugador.esAnfitrion) {
            etiquetas.push('ANFITRION');
        }

        if (esYo) {
            etiquetas.push('TU');
        }

        if (!jugador.estaConectado) {
            etiquetas.push('DESCONECTADO');
        }

        plaza.nombre.setText(jugador.nombreUsuario).setColor(css(jugador.estaConectado ? COLOR.BLANCO : COLOR.GRIS));
        plaza.etiquetas.setText(etiquetas.join(' · '));
        plaza.fondo.dibujar(COLOR.FONDO, esYo ? COLOR.MURO_CLARO : null, 2);
    }

    private codigoRol(bando: Bando | null): CodigoRol {
        if (bando === Bando.PacMan) {
            return 'PACMAN';
        }

        return bando === Bando.Fantasma ? 'FANTASMA' : null;
    }

    private mostrarMensaje(texto: string): void {
        this.mensaje?.setText(texto);
    }
}
