import { TDificultad } from '../modelos/dificultad.model';
import { TObjeto } from '../modelos/objeto.model';
import { IRespuesta } from '../modelos/respuesta.model';
import { TUsuario } from '../modelos/usuario.model';
import { URL_API } from './Configuracion';
import { SugerenciasNombre } from '../modelos/sugerencias-nombre.model';


// Lo que devuelve api/Usuario/IniciarSesion (clsSesionIniciada)
export interface SesionIniciada {
    usuario: TUsuario;
    token: string;
}

const CLAVE_SESION = 'pacman.sesion';
export const CODIGO_CORREO_NO_VERIFICADO = 'CORREO_NO_VERIFICADO';

// ---------- Sesión guardada en el dispositivo ----------

export function obtenerSesion(): SesionIniciada | null {
    try {
        const guardada = localStorage.getItem(CLAVE_SESION);

        return guardada ? (JSON.parse(guardada) as SesionIniciada) : null;
    } catch {
        return null;
    }
}

export function guardarSesion(sesion: SesionIniciada): void {
    try {
        localStorage.setItem(CLAVE_SESION, JSON.stringify(sesion));
    } catch {
        // Sin almacenamiento la sesión dura lo que la pestaña
    }
}

export function borrarSesion(): void {
    try {
        localStorage.removeItem(CLAVE_SESION);
    } catch {
        // Nada que borrar
    }
}

export function obtenerToken(): string | null {
    return obtenerSesion()?.token ?? null;
}

export function actualizarUsuarioSesion(usuario: TUsuario): void{
    const sesion = obtenerSesion();

    if (sesion) {
        guardarSesion({...sesion, usuario});

    }

}

// ---------- Llamadas REST ----------

// Todas las respuestas de la API son Respuesta<T>, también los BadRequest.
// Los fallos de red se devuelven con la misma forma para tratarlos igual.
async function solicitar<T>(metodo: 'GET' | 'POST', ruta: string, cuerpo?: unknown): Promise<IRespuesta<T>> {
    const encabezados: Record<string, string> = {};
    const token = obtenerToken();

    if (token) {
        encabezados.Authorization = `Bearer ${token}`;
    }

    if (cuerpo !== undefined) {
        encabezados['Content-Type'] = 'application/json';
    }

    try {
        const respuesta = await fetch(`${URL_API}/${ruta}`, {
            method: metodo,
            headers: encabezados,
            body: cuerpo === undefined ? undefined : JSON.stringify(cuerpo),
        });

        return (await respuesta.json()) as IRespuesta<T>;
    } catch {
        return { success: false, data: null, error: 'No se pudo conectar con el servidor.' };
    }
}

export function iniciarSesion(usuario: string, contrasena: string): Promise<IRespuesta<SesionIniciada>> {
    return solicitar<SesionIniciada>('POST', 'api/Usuario/IniciarSesion', { usuario, contrasena });
}

// Comprueba que el token guardado siga vigente y devuelve su usuario
export function validarSesion(): Promise<IRespuesta<TUsuario>> {
    return solicitar<TUsuario>('GET', 'api/Usuario/Sesion');
}

export function listarDificultades(): Promise<IRespuesta<TDificultad[]>> {
    return solicitar<TDificultad[]>('GET', 'api/Dificultad/Listar');
}

// Catálogo de objetos de las cajas, para mostrar sus nombres en la partida
export function listarObjetos(): Promise<IRespuesta<TObjeto[]>> {
    return solicitar<TObjeto[]>('GET', 'api/Objeto/Listar');
}

export function registrar(correoElectronico: string, contrasena: string): Promise<IRespuesta<TUsuario>> {
    return solicitar<TUsuario>('POST', 'api/Usuario/Registrar', { correoElectronico, contrasena });
}

// Si el código es correcto devuelve la sesión iniciada (con token)
export function verificarCorreo(correoElectronico: string, codigo: string): Promise<IRespuesta<SesionIniciada>> {
    return solicitar<SesionIniciada>('POST', 'api/Usuario/VerificarCorreo', { correoElectronico, codigo });
}

export function reenviarCodigo(correoElectronico: string): Promise<IRespuesta<boolean>> {
    return solicitar<boolean>('POST', 'api/Usuario/ReenviarCodigo', { correoElectronico });
}

export function sugerirNombres(nombre: string): Promise<IRespuesta<SugerenciasNombre>> {
    return solicitar<SugerenciasNombre>('GET', `api/Usuario/SugerirNombres?nombre=${encodeURIComponent(nombre)}`);
}

export function asignarNombre(nombreUsuario: string): Promise<IRespuesta<TUsuario>> {
    return solicitar<TUsuario>('POST', 'api/Usuario/AsignarNombre', { nombreUsuario });
}

// Cierra la sesión en el servidor y borra la guardada. Aunque el servidor no
// responda, en el dispositivo se borra igual.
export async function cerrarSesion(): Promise<void> {
    await solicitar<boolean>('POST', 'api/Usuario/CerrarSesion');
    borrarSesion();
}
