// Dirección del backend (PacmanApi). Se puede cambiar desde la app (botón
// SERVIDOR del inicio de sesión) y queda guardada en el dispositivo, así el
// mismo APK sirve en cualquier red. Si no se eligió ninguna se usa:
// - en la app de Android, VITE_API_URL de .env.android.local al compilar (ver ANDROID.md);
// - en el navegador, el mismo equipo que sirve la página, así un celular que
//   abre http://<ip-del-pc>:8080 llega a http://<ip-del-pc>:5253.
export const URL_API_PREDETERMINADA: string =
    import.meta.env.VITE_API_URL ?? `http://${window.location.hostname}:5253`;

export const PUERTO_API = 5253;

export const RUTA_HUB_JUEGO = '/hubs/juego';

const CLAVE_SERVIDOR = 'pacman.servidor';

export function obtenerUrlApi(): string {
    try {
        return localStorage.getItem(CLAVE_SERVIDOR) ?? URL_API_PREDETERMINADA;
    } catch {
        return URL_API_PREDETERMINADA;
    }
}

// null vuelve a la dirección predeterminada
export function guardarUrlApi(url: string | null): void {
    try {
        if (url === null) {
            localStorage.removeItem(CLAVE_SERVIDOR);
        } else {
            localStorage.setItem(CLAVE_SERVIDOR, url);
        }
    } catch {
        // Sin almacenamiento solo queda la predeterminada
    }
}

// Acepta lo que escribe el jugador: "192.168.1.5", "192.168.1.5:5253", la
// dirección de un túnel de Visual Studio (con o sin https://) o una URL completa.
// Devuelve el origen (http://host:puerto) o null si no es válida.
export function normalizarUrlApi(texto: string): string | null {
    const limpio = texto.trim().replace(/\/+$/, '');

    if (!limpio) {
        return null;
    }

    // Los túneles solo responden por https
    const esquema = /\.devtunnels\.ms$/i.test(limpio) ? 'https' : 'http';
    const conEsquema = /^[a-z][a-z0-9+.-]*:\/\//i.test(limpio) ? limpio : `${esquema}://${limpio}`;

    try {
        const url = new URL(conEsquema);

        if ((url.protocol !== 'http:' && url.protocol !== 'https:') || !url.hostname ||
            url.username || url.password || url.pathname !== '/' || url.search || url.hash) {
            return null;
        }

        // Sin puerto escrito se usa el de PacmanApi (salvo que sea https, p. ej. un túnel)
        if (!url.port && url.protocol === 'http:' && !/:\d+$/.test(limpio)) {
            url.port = String(PUERTO_API);
        }

        return url.origin;
    } catch {
        return null;
    }
}
