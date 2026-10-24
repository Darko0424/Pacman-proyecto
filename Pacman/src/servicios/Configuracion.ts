// Dirección del backend (PacmanApi). En el navegador se usa el mismo equipo que
// sirve la página, así un celular que abre http://<ip-del-pc>:8080 llega a
// http://<ip-del-pc>:5253. En la app de Android hay que fijar VITE_API_URL en
// .env.android.local al compilar en modo android (ver ANDROID.md).
export const URL_API: string =
    import.meta.env.VITE_API_URL ?? `http://${window.location.hostname}:5253`;

export const RUTA_HUB_JUEGO = '/hubs/juego';
