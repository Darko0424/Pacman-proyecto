import { Scene, Sound } from 'phaser';
import { TipoEventoJuego } from '../../modelos/tiempo-real/enums';
import { EventoDto } from '../../modelos/tiempo-real/estado-partida.dto';
import { ClaveSonido, SONIDO } from './CatalogoSonidos';

// Sonido de cada evento que manda el servidor. Los que no están aquí
// (choques, objetos usados, efectos que terminan) no tienen sonido todavía.
const SONIDO_EVENTO: Partial<Record<TipoEventoJuego, ClaveSonido>> = {
    [TipoEventoJuego.BolaPoderComida]: SONIDO.COMER_FRUTA,
    [TipoEventoJuego.CajaRecogida]: SONIDO.COMER_FRUTA,
    [TipoEventoJuego.FantasmaComido]: SONIDO.COMER_FANTASMA,
    [TipoEventoJuego.PacManAtrapado]: SONIDO.MUERTE,
    [TipoEventoJuego.Reaparicion]: SONIDO.VIDA,
    [TipoEventoJuego.NivelTerminado]: SONIDO.NIVEL_COMPLETADO,
};

// Reproduce los sonidos de la partida a partir de los eventos de cada estado
export class GestorSonido {
    // El waka suena seguido mientras Pac-Man come: una sola instancia que no se
    // reinicia si ya está sonando
    private readonly waka: Sound.BaseSound;
    private musica: Sound.BaseSound | null = null;

    constructor(private readonly escena: Scene) {
        this.waka = escena.sound.add(SONIDO.WAKA, { volume: 0.35 });
    }

    reproducirEventos(eventos: EventoDto[]): void {
        // Varios eventos iguales en el mismo estado suenan una sola vez
        const claves = new Set<ClaveSonido>();

        for (const evento of eventos) {
            if (evento.tipo === TipoEventoJuego.BolaComida) {
                if (!this.waka.isPlaying) {
                    this.waka.play();
                }

                continue;
            }

            const clave = SONIDO_EVENTO[evento.tipo];
            if (clave) {
                claves.add(clave);
            }
        }

        claves.forEach((clave) => this.escena.sound.play(clave, { volume: 0.6 }));
    }

    // Música entre niveles
    intermision(): void {
        this.detenerMusica();
        this.musica = this.escena.sound.add(SONIDO.INTERMISION, { volume: 0.5 });
        this.musica.play();
    }

    detenerMusica(): void {
        this.musica?.stop();
        this.musica?.destroy();
        this.musica = null;
    }

    destruir(): void {
        this.detenerMusica();
        this.waka.destroy();
    }
}
