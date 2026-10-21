export interface TUsuario {
    idUsuario: number;
    nombreUsuario: string;
    correoElectronico: string;
    fechaRegistro?: string | Date;
    nombreDefinido: boolean;
    contrasena?: string;
}
