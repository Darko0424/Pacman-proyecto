import { ANCHO_INTERFAZ, ALTO_INTERFAZ } from '../ui/PantallaAdaptable';
import { GameObjects, Scene, Time } from 'phaser';
import { EstadoSala } from '../../modelos/tiempo-real/enums';
import { SalaDto } from '../../modelos/tiempo-real/sala.dto';
import { obtenerSesion } from '../../servicios/ClienteApi';
import { conexionTiempoReal } from '../../servicios/ConexionTiempoReal';
import { SpriteFantasma } from '../sprites/SpriteFantasma';
import { SpritePacMan } from '../sprites/SpritePacMan';
import { Boton } from '../ui/Boton';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { Recuadro, crearPanel, dibujarMarco } from '../ui/Panel';

// GestorSalas admite como máximo cinco salas a la vez
const CANTIDAD_MAXIMA_SALAS = 5;

// ListarSalas no llega por aviso del servidor: se vuelve a pedir cada tanto
const INTERVALO_ACTUALIZAR_MS = 3000;

const ANCHO_FILA = 820;
const ALTO_FILA = 80;
const SEPARACION_FILA = 12;
const PRIMERA_FILA_Y = 170;


interface AspectoEstado {
    texto: string;
    color: number;
}

const ASPECTO_ESTADO: Record<EstadoSala, AspectoEstado> = {
    [EstadoSala.Lobby]: { texto: 'EN ESPERA', color: COLOR.VERDE },
    [EstadoSala.Jugando]: { texto: 'JUGANDO', color: COLOR.NARANJA },
    [EstadoSala.Transicion]: { texto: 'ENTRE NIVELES', color: COLOR.NARANJA },
    [EstadoSala.Finalizada]: { texto: 'FINALIZADA', color: COLOR.GRIS },
};

// Colores de los cuadritos que marcan cada plaza ocupada
const COLORES_PLAZA = [COLOR.PACMAN, COLOR.VERDE, COLOR.ROJO, COLOR.ROSA];

// Buscar sala: lista las salas abiertas, permite crear una o unirse a otra.
export class EscenaListaSalas extends Scene {
    private filas: GameObjects.Container;
    private contador: GameObjects.Text;
    private mensaje: GameObjects.Text;
    private botonCrear: Boton;
    private botonActualizar: Boton;
    private temporizador: Time.TimerEvent | null = null;
    private ocupado = false;

    constructor() {
        super('EscenaListaSalas');
    }

    create(): void {
        const width = ANCHO_INTERFAZ;

        // El marco va encima para que los personajes entren y salgan por detrás
        this.dibujarDesfile();
        dibujarMarco(this);

        this.add.text(width / 2, 64, 'BUSCAR SALA', estiloTexto(32, COLOR.PACMAN)).setOrigin(0.5);
        this.contador = this.add.text(width / 2, 112, '', estiloTexto(12, COLOR.GRIS_CLARO)).setOrigin(0.5);

        this.filas = this.add.container(0, 0);
        this.mensaje = this.add.text(width / 2, 622, '', estiloTexto(12, COLOR.ROSA)).setOrigin(0.5);

        new Boton(this, 230, 680, 'VOLVER', {
            ancho: 220,
            color: COLOR.GRIS_CLARO,
            alPulsar: () => this.volverAlMenu(),
        });

        this.botonActualizar = new Boton(this, width / 2, 680, 'ACTUALIZAR', {
            ancho: 240,
            alPulsar: () => this.actualizar(),
        });

        this.botonCrear = new Boton(this, width - 230, 680, 'CREAR SALA', {
            ancho: 260,
            color: COLOR.PACMAN,
            alPulsar: () => this.crearSala(),
        });

        this.events.once('shutdown', () => this.temporizador?.remove());

        if (!obtenerSesion()) {
            this.botonCrear.setHabilitado(false);
            this.botonActualizar.setHabilitado(false);
            this.mostrarVacio('Inicia sesión para jugar en línea');

            return;
        }

        this.entrar();
    }

    // Si el usuario ya tenía plaza en una sala (perdió la señal, cerró la app)
    // vuelve directo a ella; si no, muestra la lista.
    private async entrar(): Promise<void> {
        this.mostrarVacio('Conectando...');

        const conexion = await conexionTiempoReal.conectar();

        // La escena pudo cerrarse mientras esperaba (por ejemplo con volver)
        if (!this.sys.isActive()) {
            return;
        }

        if (!conexion.success) {
            this.mostrarVacio('Sin conexión con el servidor');
            this.mostrarError(conexion.error);

            return;
        }

        const reanudada = await conexionTiempoReal.reanudarSala();

        if (!this.sys.isActive()) {
            return;
        }

        if (reanudada.success && reanudada.data && reanudada.data.estado !== EstadoSala.Finalizada) {
            this.irASala(reanudada.data);

            return;
        }

        await this.salirDeSalasTerminadas();

        if (!this.sys.isActive()) {
            return;
        }

        await this.actualizar();

        this.temporizador = this.time.addEvent({
            delay: INTERVALO_ACTUALIZAR_MS,
            loop: true,
            callback: () => this.actualizar(),
        });
    }

    // Una sala finalizada conserva a sus jugadores hasta que salen, y mientras
    // tanto no pueden crear ni unirse a otra. ReanudarSala no la devuelve, así
    // que se busca en la lista.
    private async salirDeSalasTerminadas(): Promise<void> {
        const idUsuario = obtenerSesion()?.usuario.idUsuario;
        const respuesta = await conexionTiempoReal.listarSalas();
        const terminadas = (respuesta.data ?? []).filter((sala) =>
            sala.estado === EstadoSala.Finalizada &&
            sala.jugadores.some((jugador) => jugador.idUsuario === idUsuario));

        for (const sala of terminadas) {
            await conexionTiempoReal.salirSala(sala.codigo);
        }
    }

    private async actualizar(): Promise<void> {
        const respuesta = await conexionTiempoReal.listarSalas();

        // La escena pudo cerrarse mientras esperaba la respuesta
        if (!this.sys.isActive()) {
            return;
        }

        if (!respuesta.success || !respuesta.data) {
            this.mostrarError(respuesta.error);

            return;
        }

        this.mostrarSalas(respuesta.data);
    }

    private async crearSala(): Promise<void> {
        await this.ejecutar(() => conexionTiempoReal.crearSala());
    }

    private async unirse(codigo: string): Promise<void> {
        await this.ejecutar(() => conexionTiempoReal.unirseSala(codigo));
    }

    // Crear y unirse terminan igual: se entra a la sala o se muestra el motivo
    private async ejecutar(operacion: () => ReturnType<typeof conexionTiempoReal.crearSala>): Promise<void> {
        if (this.ocupado) {
            return;
        }

        this.ocupado = true;
        this.mostrarError('');

        const respuesta = await operacion();
        this.ocupado = false;

        if (!this.sys.isActive()) {
            return;
        }

        if (respuesta.success && respuesta.data) {
            this.irASala(respuesta.data);
        } else {
            this.mostrarError(respuesta.error);
            this.actualizar();
        }
    }

    private irASala(sala: SalaDto): void {
        this.scene.start('EscenaSala', { sala });
    }

    private volverAlMenu(): void {
        this.scene.start('EscenaMenu');
    }

    // ---------- Dibujo ----------

    private mostrarSalas(salas: SalaDto[]): void {
        this.filas.removeAll(true);
        this.contador.setText(`SALAS ACTIVAS: ${salas.length}/${CANTIDAD_MAXIMA_SALAS}`);
        this.botonCrear.setHabilitado(salas.length < CANTIDAD_MAXIMA_SALAS);
        this.botonActualizar.setHabilitado(true);

        if (salas.length === 0) {
            this.mostrarVacio('No hay salas abiertas', '¡Crea la primera!');

            return;
        }

        // Primero las que admiten jugadores
        const ordenadas = [...salas].sort((a, b) => Number(this.admiteIngreso(b)) - Number(this.admiteIngreso(a)));

        ordenadas.forEach((sala, i) => {
            this.filas.add(this.crearFila(sala, PRIMERA_FILA_Y + i * (ALTO_FILA + SEPARACION_FILA)));
        });
    }

    private crearFila(sala: SalaDto, y: number): GameObjects.Container {
        const width = ANCHO_INTERFAZ;
        const x = (width - ANCHO_FILA) / 2;
        const fila = this.add.container(x, y);
        const aspecto = ASPECTO_ESTADO[sala.estado];
        const centro = ALTO_FILA / 2;

        const fondo = crearPanel(this, 0, 0, ANCHO_FILA, ALTO_FILA);
        const codigo = this.add.text(28, centro - 12, sala.codigo, estiloTexto(20, COLOR.PACMAN)).setOrigin(0, 0.5);
        const estado = this.add.text(28, centro + 16, aspecto.texto, estiloTexto(10, aspecto.color)).setOrigin(0, 0.5);
        fila.add([fondo, codigo, estado]);

        // Una plaza por jugador posible: llena con color vacía solo con borde
        for (let i = 0; i < sala.capacidadHumanos; i++) {
            const ocupada = i < sala.jugadores.length;
            const color = COLORES_PLAZA[i % COLORES_PLAZA.length];
            const plaza = new Recuadro(this, 260 + i * 26, centro - 12, 18, 18, 4, true)
                .dibujar(ocupada ? color : null, ocupada ? color : COLOR.GRIS, 2);
            fila.add(plaza);
        }

        const cantidad = this.add
            .text(260 + sala.capacidadHumanos * 26 + 6, centro - 12, `${sala.jugadores.length}/${sala.capacidadHumanos}`,
                estiloTexto(12, COLOR.BLANCO))
            .setOrigin(0, 0.5);

        const anfitrion = sala.jugadores.find((jugador) => jugador.esAnfitrion);
        const detalle = sala.estado === EstadoSala.Lobby
            ? `ANFITRION: ${anfitrion?.nombreUsuario ?? '-'}`
            : `NIVEL ${sala.numeroNivel}/${sala.cantidadNiveles}`;
        const textoDetalle = this.add
            .text(251, centro + 16, detalle, estiloTexto(10, COLOR.GRIS_CLARO))
            .setOrigin(0, 0.5);

        fila.add([cantidad, textoDetalle]);

        if (this.admiteIngreso(sala)) {
            fila.add(new Boton(this, ANCHO_FILA - 100, centro, 'UNIRSE', {
                ancho: 160,
                alto: 48,
                tamanoTexto: 12,
                color: COLOR.VERDE,
                alPulsar: () => this.unirse(sala.codigo),
            }));
        } else {
            const motivo = sala.estado === EstadoSala.Lobby ? 'LLENA' : 'EN CURSO';
            fila.add(this.add.text(ANCHO_FILA - 100, centro, motivo, estiloTexto(12, COLOR.GRIS)).setOrigin(0.5));
        }

        return fila;
    }

    private admiteIngreso(sala: SalaDto): boolean {
        return sala.estado === EstadoSala.Lobby && sala.jugadores.length < sala.capacidadHumanos;
    }

    private mostrarVacio(titulo: string, subtitulo = ''): void {
        const width = ANCHO_INTERFAZ;

        this.filas.removeAll(true);
        this.filas.add([
            new SpriteFantasma(this, width / 2, 330, 'CELESTE').setScale(3).mirar('ABAJO'),
            this.add.text(width / 2, 410, titulo, estiloTexto(16, COLOR.BLANCO)).setOrigin(0.5),
            this.add.text(width / 2, 446, subtitulo, estiloTexto(12, COLOR.PACMAN)).setOrigin(0.5),
        ]);
    }

    private mostrarError(texto: string): void {
        this.mensaje.setText(texto);
    }

    // Animacion de los fantasmas huyendo de pacman
    private dibujarDesfile(): void {
        const width = ANCHO_INTERFAZ;
        const height = ALTO_INTERFAZ;
        const y = height - 34;
        const grupo = this.add.container(-60, y);

        grupo.add([
            new SpritePacMan(this, 0, 0, 'AMARILLO').mirar('DERECHA').comer(),
            new SpriteFantasma(this, 70, 0, 'ROJO').mirar('DERECHA'),
            new SpriteFantasma(this, 106, 0, 'ROSA').mirar('DERECHA'),
            new SpriteFantasma(this, 142, 0, 'CELESTE').mirar('DERECHA'),
        ]);

        // Los fantasmas van delante y Pac-Man detras  al salir vuelven a empezar
        grupo.list.forEach((actor) => {
            if (actor instanceof SpriteFantasma) {
                actor.establecerModo('ASUSTADO');
            }
        });

        this.tweens.add({
            targets: grupo,
            x: width + 60,
            duration: 9000,
            repeat: -1,
            repeatDelay: 1500,
        });
    }
}
