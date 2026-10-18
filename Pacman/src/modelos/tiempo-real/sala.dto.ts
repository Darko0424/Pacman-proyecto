import { Bando, EstadoSala, FrecuenciaCajas, ModoPartida } from './enums';

// Opciones del menú de la sala (ConfiguracionSalaDto). Solo las cambia el anfitrión.
export interface ConfiguracionSalaDto {
    idDificultad: number;
    // Entre 1 y 5; las tienen Pac-Man y fantasmas, y se reinician en cada nivel
    vidas: number;
    colisionEntrePacMan: boolean;
    frecuenciaCajas: FrecuenciaCajas;
}

export interface JugadorSalaDto {
    idUsuario: number;
    nombreUsuario: string;
    // Personaje del nivel en juego; null en el lobby
    idPersonaje: number | null;
    estaConectado: boolean;
    esAnfitrion: boolean;
    // Solo cuenta si juega solo contra la IA; null = sorteo
    rolPreferido: Bando | null;
}

export interface SalaDto {
    codigo: string;
    estado: EstadoSala;
    idAnfitrion: number | null;
    // Se fija al iniciar según cuántos jugadores había
    modo: ModoPartida | null;
    numeroNivel: number;
    cantidadNiveles: number;
    puedeIniciar: boolean;
    capacidadHumanos: number;
    configuracion: ConfiguracionSalaDto;
    // En orden de ingreso
    jugadores: JugadorSalaDto[];
}
