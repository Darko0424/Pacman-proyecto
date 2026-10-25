# Pac-Man Multijugador: cliente

Cliente del juego hecho con [Phaser 4](https://phaser.io), TypeScript y Vite. Se
juega en el navegador o como app de Android (Capacitor). Toda la partida la
simula `PacmanApi` en la PC; el cliente solo dibuja lo que recibe por SignalR y
envía las direcciones del jugador.

## Requisitos

[Node.js](https://nodejs.org) 22 o posterior. Instalar dependencias con `npm ci`.

## Comandos

| Comando | Qué hace |
| --- | --- |
| `npm run dev` | Servidor de desarrollo en `http://localhost:8080` (también accesible desde la red local). |
| `npm run build` | Compilación de producción en `dist`. |
| `npm run android:apk` | Genera el APK de prueba (ver [ANDROID.md](ANDROID.md)). |
| `npm run android:open` | Abre el proyecto nativo en Android Studio. |

## Servidor

En el navegador el cliente busca la API en el mismo equipo que sirve la página,
puerto 5253. En la app de Android la dirección se elige en la pantalla de inicio
de sesión (botón **SERVIDOR**) y queda guardada en el teléfono; ver
[ANDROID.md](ANDROID.md).

## Estructura

| Carpeta | Contenido |
| --- | --- |
| `src/game/main.ts` | Configuración de Phaser y lista de escenas. |
| `src/presentacion/escenas` | Pantallas: sesión, menú, salas, ranking, historial y partida. |
| `src/presentacion/sprites`, `ui`, `sonidos`, `animaciones` | Vistas, componentes de interfaz y audio. |
| `src/logica` | Estado del cliente, interpolación y efectos visuales. |
| `src/servicios` | `ClienteApi` (REST), `ConexionTiempoReal` (SignalR) y la dirección del servidor. |
| `src/modelos` | Modelos que llegan de la API. |
| `public/assets` | Sprites y sonidos. |
| `android` | Proyecto nativo de Capacitor. |

## Accesos de desarrollo

Solo con `npm run dev`: `?sprites` abre la galería de sprites y `?salas` va
directo a la lista de salas (por ejemplo `http://localhost:8080/?sprites`).
