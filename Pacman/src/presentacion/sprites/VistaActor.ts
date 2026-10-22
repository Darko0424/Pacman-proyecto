import { GameObjects, Scene, TintModes } from 'phaser';
import { EstadoVisual } from '../../logica/EfectosVisuales';
import { ActorDto, nombreActor } from '../../logica/EstadoCliente';
import { Direccion as DireccionDto } from '../../modelos/tiempo-real/enums';
import { FantasmaDto } from '../../modelos/tiempo-real/estado-partida.dto';
import { COLOR, estiloTexto } from '../ui/Paleta';
import { Recuadro } from '../ui/Panel';
import { Direccion, colorPersonaje } from './CatalogoSprites';
import { ModoFantasma, SpriteFantasma } from './SpriteFantasma';
import { SpritePacMan } from './SpritePacMan';

const DIRECCION_SPRITE: Partial<Record<DireccionDto, Direccion>> = {
    [DireccionDto.Arriba]: 'ARRIBA',
    [DireccionDto.Abajo]: 'ABAJO',
    [DireccionDto.Izquierda]: 'IZQUIERDA',
    [DireccionDto.Derecha]: 'DERECHA',
};

// Parpadeos por segundo mientras es invencible o está aturdido
const PARPADEO_HZ = 8;

// Cada cuánto deja una imagen el rastro de la velocidad (segundos)
const INTERVALO_RASTRO = 0.06;

// Un personaje en el mapa: su sprite, su nombre encima y lo que muestran los
// efectos. Recibe el estado del servidor y solo decide cómo se ve; dónde está
// lo calcula la interpolación.
export class VistaActor {
    private readonly contenedor: GameObjects.Container;
    private readonly pacman: SpritePacMan | null;
    private readonly fantasma: SpriteFantasma | null;
    private readonly etiqueta: GameObjects.Text;

    // Marcas de los efectos
    private readonly hielo: Recuadro;
    private readonly escudo: GameObjects.Arc;
    private readonly dobles: GameObjects.Text;

    private direccion: Direccion = 'IZQUIERDA';
    private direccionFantasma: Direccion | null = null;
    private modo: ModoFantasma | null = null;
    private muriendo = false;
    private moviendose = false;
    private tinte = '';
    private ultimoRastro = 0;

    constructor(
        private readonly escena: Scene,
        readonly idPersonaje: number,
        esPacMan: boolean,
        private readonly esPropio: boolean,
        private readonly escala: number,
    ) {
        const color = colorPersonaje(idPersonaje);
        const lado = 30 * escala;

        this.contenedor = escena.add.container(0, 0).setDepth(esPacMan ? 2 : 3);

        if (esPacMan) {
            this.pacman = new SpritePacMan(escena, 0, 0, color).setScale(escala);
            this.fantasma = null;
        } else {
            this.fantasma = new SpriteFantasma(escena, 0, 0, color).setScale(escala);
            this.pacman = null;
        }

        this.hielo = new Recuadro(escena, 0, 0, lado, lado, 6, true).dibujar(COLOR.HIELO, COLOR.BLANCO, 2);
        this.hielo.setAlpha(0.45).setVisible(false);

        this.escudo = escena.add.circle(0, 0, 18 * escala).setStrokeStyle(2, COLOR.PACMAN).setVisible(false);
        escena.tweens.add({ targets: this.escudo, scale: 1.15, alpha: 0.5, duration: 400, yoyo: true, repeat: -1 });

        this.dobles = escena.add
            .text(14 * escala, 10 * escala, 'x2', estiloTexto(8, COLOR.PACMAN))
            .setOrigin(0.5)
            .setStroke('#272736', 4)
            .setVisible(false);

        this.etiqueta = escena.add
            .text(0, -20 * escala, '', estiloTexto(8, esPropio ? COLOR.PACMAN : COLOR.GRIS_CLARO))
            .setOrigin(0.5, 1)
            .setStroke('#272736', 4);

        this.contenedor.add([this.escudo, this.sprite, this.hielo, this.dobles, this.etiqueta]);
    }

    private get sprite(): SpritePacMan | SpriteFantasma {
        return (this.pacman ?? this.fantasma)!;
    }

    actualizar(actor: ActorDto, x: number, y: number, ahora: number, visual: EstadoVisual): void {
        this.contenedor.setPosition(x, y);
        this.etiqueta.setText(this.esPropio ? 'TU' : nombreActor(actor));

        // Un fantasma sin vidas queda fuera del nivel; Pac-Man antes anima su muerte
        if (actor.eliminado && this.fantasma) {
            this.contenedor.setVisible(false);
            return;
        }

        this.contenedor.setVisible(true);

        const direccion = DIRECCION_SPRITE[actor.direccion];
        if (direccion) {
            this.direccion = direccion;
        }

        if (this.pacman) {
            this.actualizarPacMan(actor);
        } else {
            this.actualizarFantasma(actor as FantasmaDto);
        }

        const sprite = this.sprite;
        const parpadea = actor.esInvencible || (actor as FantasmaDto).aturdido;
        const encendido = !parpadea || Math.floor(ahora * PARPADEO_HZ) % 2 === 0;

        sprite.setAlpha(encendido ? 1 : 0.35);
        this.etiqueta.setVisible(sprite.visible);
        this.mostrarEfectos(visual, sprite.visible, x, y, ahora);
    }

    destruir(): void {
        this.contenedor.destroy();
    }

    // ---------- Efectos ----------

    private mostrarEfectos(visual: EstadoVisual, visible: boolean, x: number, y: number, ahora: number): void {
        this.hielo.setVisible(visible && visual.congelado);
        this.escudo.setVisible(visible && visual.escudo);
        this.dobles.setVisible(visible && visual.puntosDobles);

        // Congelado: silueta celeste dentro del bloque de hielo y sin animar.
        // Ralentizado: morado y a media animación.
        const tinte = visual.congelado ? 'HIELO' : visual.ralentizado ? 'LENTO' : '';
        if (tinte !== this.tinte) {
            this.tinte = tinte;
            this.aplicarTinte(tinte);
        }

        if (visible && visual.acelerado && !visual.congelado && ahora - this.ultimoRastro >= INTERVALO_RASTRO) {
            this.ultimoRastro = ahora;
            this.dejarRastro(x, y);
        }
    }

    private aplicarTinte(tinte: string): void {
        const velocidad = tinte === 'HIELO' ? 0 : tinte === 'LENTO' ? 0.5 : 1;

        if (this.pacman) {
            if (tinte === 'HIELO') {
                this.pacman.setTint(COLOR.HIELO).setTintMode(TintModes.FILL);
            } else if (tinte === 'LENTO') {
                this.pacman.setTint(COLOR.LENTO).setTintMode(TintModes.MULTIPLY);
            } else {
                this.pacman.clearTint().setTintMode(TintModes.MULTIPLY);
            }

            this.pacman.anims.timeScale = velocidad;
        } else {
            const fantasma = this.fantasma!;

            if (tinte === 'HIELO') {
                fantasma.tenir(COLOR.HIELO, TintModes.FILL);
            } else {
                fantasma.tenir(tinte === 'LENTO' ? COLOR.LENTO : null);
            }

            fantasma.setVelocidadAnimacion(velocidad);
        }
    }

    // Copia del cuadro actual que se desvanece detrás del personaje
    private dejarRastro(x: number, y: number): void {
        const { textura, cuadro } = this.pacman
            ? { textura: this.pacman.texture.key, cuadro: this.pacman.frame.name }
            : this.fantasma!.cuadroCuerpo();

        const copia = this.escena.add
            .image(x, y, textura, cuadro)
            .setScale(this.escala)
            .setAngle(this.pacman?.angle ?? 0)
            .setFlipX(this.pacman?.flipX ?? false)
            .setAlpha(0.45)
            .setDepth(1);

        this.escena.tweens.add({ targets: copia, alpha: 0, duration: 250, onComplete: () => copia.destroy() });
    }

    // ---------- Personajes ----------

    private actualizarPacMan(actor: ActorDto): void {
        const pacman = this.pacman!;

        // Atrapado: anima la muerte una vez y queda oculto hasta reaparecer
        if (actor.reaparicionPendiente || actor.eliminado) {
            if (!this.muriendo) {
                this.muriendo = true;
                pacman.morir();
                pacman.once('animationcomplete', () => pacman.setVisible(false));
            }

            return;
        }

        if (this.muriendo) {
            this.muriendo = false;
            this.moviendose = false;
            pacman.setVisible(true);
        }

        pacman.mirar(this.direccion);

        const moviendose = actor.direccion !== DireccionDto.Ninguna;
        if (moviendose !== this.moviendose) {
            this.moviendose = moviendose;

            if (moviendose) {
                pacman.comer();
            } else {
                pacman.detener();
            }
        }
    }

    private actualizarFantasma(actor: FantasmaDto): void {
        const fantasma = this.fantasma!;

        // Comido: solo los ojos hasta que reaparece
        const modo: ModoFantasma = actor.reaparicionPendiente ? 'COMIDO' : actor.asustado ? 'ASUSTADO' : 'NORMAL';

        if (modo !== this.modo) {
            this.modo = modo;
            fantasma.establecerModo(modo);
        }

        if (this.direccion !== this.direccionFantasma) {
            this.direccionFantasma = this.direccion;
            fantasma.mirar(this.direccion);
        }

        // Aturdido tras chocar con otro fantasma: se tambalea
        fantasma.setAngle(actor.aturdido ? Math.sin(this.escena.time.now / 60) * 15 : 0);
    }
}
