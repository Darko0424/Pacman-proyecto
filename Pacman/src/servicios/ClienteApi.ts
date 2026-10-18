import { TDificultad } from '../modelos/dificultad.model';
import { IRespuesta } from '../modelos/respuesta.model';
import { TUsuario } from '../modelos/usuario.model';
import { URL_API } from './Configuracion';

// Lo que devuelve api/Usuario/IniciarSesion (clsSesionIniciada)
export interface SesionIniciada {
    usuario: TUsuario;
    token: string;
}

const CLAVE_SESION = 'pacman.sesion';

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
