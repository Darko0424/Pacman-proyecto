import { Bando } from '../modelos/tiempo-real/enums';
import { EstadoPartidaDto, FantasmaDto, PacManDto } from '../modelos/tiempo-real/estado-partida.dto';
import { NivelDto } from '../modelos/tiempo-real/nivel.dto';
import { SalaDto } from '../modelos/tiempo-real/sala.dto';
import { Interpolacion } from './Interpolacion';

export type ActorDto = PacManDto | FantasmaDto;

export interface ActorConBando {
    actor: ActorDto;
    bando: Bando;
}

// Lo que el celular sabe de la partida en curso: lo último que mandó el
// servidor (sala, nivel, estados) y quién es el jugador de este dispositivo.
// El cliente nunca calcula reglas: solo guarda y consulta.
export class EstadoCliente {
    sala: SalaDto | null = null;
    nivel: NivelDto | null = null;
    readonly interpolacion = new Interpolacion();

    constructor(
        readonly codigoSala: string,
        readonly idUsuario: number,
    ) {}

    get estado(): EstadoPartidaDto | null {
        return this.interpolacion.ultimo;
    }

    // Pac-Man primero, luego los fantasmas
    actores(estado: EstadoPartidaDto | null = this.estado): ActorConBando[] {
        if (!estado) {
            return [];
        }

        return [
            ...estado.pacmans.map((actor) => ({ actor, bando: Bando.PacMan })),
            ...estado.fantasmas.map((actor) => ({ actor, bando: Bando.Fantasma })),
        ];
    }

    // El personaje de este jugador en el nivel actual
    miActor(estado: EstadoPartidaDto | null = this.estado): ActorConBando | null {
        return this.actores(estado).find(({ actor }) => actor.idUsuario === this.idUsuario) ?? null;
    }

    buscarActor(estado: EstadoPartidaDto, idPersonaje: number): ActorDto | undefined {
        return this.actores(estado).find(({ actor }) => actor.idPersonaje === idPersonaje)?.actor;
    }
}

// Nombre que se muestra sobre el personaje y en el HUD
export function nombreActor(actor: ActorDto): string {
    if (actor.esIa) {
        return 'IA';
    }

    const nombre = actor.nombreUsuario ?? 'JUGADOR';

    // Desconectado o se fue: la IA lo mueve mientras tanto
    return actor.controlIa ? `${nombre} (IA)` : nombre;
}
