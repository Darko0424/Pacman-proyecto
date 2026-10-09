export interface TSesionMovil {
    idSesion: number;
    idUsuario: number;
    tokenDispositivo: string;
    ultimoAcceso?: string | Date;
}
