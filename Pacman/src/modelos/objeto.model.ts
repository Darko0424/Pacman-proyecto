export interface TObjeto {
    idObjeto: number;
    nombre: string;
    bando: 'PACMAN' | 'FANTASMA';
    efecto: string;
    duracionSeg: number;
}
