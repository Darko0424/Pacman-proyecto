import { ModoPartida } from './enums';
import { ConfiguracionSalaDto } from './sala.dto';

export interface CoordenadaDto {
    fila: number;
    columna: number;
}

export interface PuntoDto extends CoordenadaDto {
    esDePoder: boolean;
}

// Se recibe una vez al empezar cada nivel (NivelIniciado): el mapa no cambia
// durante el nivel. Lo que cambia viaja en EstadoPartidaDto.
export interface NivelDto {
    codigoSala: string | null;
    numeroNivel: number;
    modo: ModoPartida;
    filas: number;
    columnas: number;
    // Una cadena por fila: '#' muro, ' ' suelo (las bolas van en puntos)
    casillas: string[];
    puntosReaparicion: CoordenadaDto[];
    puntosCajas: CoordenadaDto[];
    puntos: PuntoDto[];
    configuracion: ConfiguracionSalaDto;
}
