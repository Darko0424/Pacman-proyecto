# Probar Pac-Man en Android

El cliente Phaser se empaqueta con Capacitor. La API de C# y SQL Server siguen ejecutándose en la PC.

## Generar el APK de prueba

Requisitos: Node.js 22 o posterior, las dependencias de `npm ci`, Android SDK Platform 36 y un JDK entre 21 y 24 compatible con Gradle 8.14.3.

Desde la carpeta `Pacman`, configura la dirección predeterminada de la API y prepara el proyecto Android:

```powershell
$env:VITE_API_URL = 'http://IP-DE-TU-PC:5253'
npm run build -- --mode android
npm run android:sync
npm run android:open
```

Sustituye `IP-DE-TU-PC` por la IPv4 del adaptador Wi-Fi; puedes verla con `ipconfig`. El celular debe poder acceder a esa misma PC. `localhost` en el celular apunta al propio teléfono. En Android Studio selecciona la variante **debug** y usa **Build → Build APK(s)**.

Como alternativa, copia `.env.android.example` a `.env.android.local` y escribe allí la URL antes de compilar. Este archivo local está ignorado por Git.

El APK de prueba queda en:

```text
Pacman/android/app/build/outputs/apk/debug/app-debug.apk
```

No se incluyen contraseñas de SQL ni del correo en el APK.

La dirección que se indica al compilar es solo la predeterminada. Si cambian de red o de PC no hace falta otro APK. Con `PacmanApi` iniciado con el perfil **red-local**, se abren Swagger y la página del QR en el navegador de la PC. Si la segunda pestaña no aparece, abre `http://localhost:5253/conectar`. En la app toca **SERVIDOR → ESCANEAR QR** y concede permiso de cámara. La app guarda la dirección y vuelve al inicio de sesión; allí se comprueba la conexión cuando se usa la cuenta. Elige el QR de la red local o el del túnel según cómo se conectará el celular.

También puedes escribir la IP en **SERVIDOR** (por ejemplo `192.168.1.100`; el puerto 5253 se agrega solo) y pulsar **GUARDAR SERVIDOR**. Esta opción funciona si la cámara no está disponible. Guardar una dirección no garantiza que la API esté encendida; si no responde, el inicio de sesión mostrará el error de conexión. **PREDETERMINADA** vuelve a la dirección incluida al compilar. La compilación del navegador conserva su configuración habitual.

Si aparece **Verificando sesión...** al abrir una APK 1.0.5 o posterior, la comprobación termina en unos seis segundos cuando la API no responde. El botón **SERVIDOR** se puede pulsar durante la espera para escanear otro QR. La dirección guardada en una instalación anterior tiene prioridad sobre la predeterminada de la APK nueva; usa **PREDETERMINADA** o **GUARDAR SERVIDOR** si cambió la IP.

## Conexión desde cualquier red con Dev Tunnels

Visual Studio puede dar a `PacmanApi` una dirección HTTPS estable para que los celulares no dependan de la IP de la Wi-Fi. Para probarla:

1. Inicia sesión en Visual Studio. En el menú de depuración, entra a **Dev Tunnels → Crear un túnel**.
2. Elige **Persistente** y, para que la APK pueda conectarse sin una cuenta de Microsoft, **Público**. Activa ese túnel en el mismo menú.
3. Inicia o reinicia `PacmanApi` con el perfil **red-local**. La página `http://localhost:5253/conectar` mostrará primero la tarjeta **Internet: túnel de Visual Studio**.
4. En la APK 1.0.4 o posterior, toca **SERVIDOR → ESCANEAR QR** y lee esa tarjeta. La dirección queda guardada en el celular. También puedes escribir a mano la URL HTTPS del túnel y pulsar **GUARDAR SERVIDOR** en la versión 1.0.6 o posterior.

La PC, Visual Studio, `PacmanApi` y SQL Server deben seguir activos durante la partida. Un túnel público permite que otras personas lleguen a la API por Internet; úsalo solo para las pruebas del equipo y desactívalo al terminar. Si todos juegan en la misma Wi-Fi, la tarjeta de red local evita el recorrido por Internet.

## Iniciar el servidor y probar

1. En Visual Studio, selecciona `PacmanApi` y el perfil **red-local**. Este perfil escucha en `http://0.0.0.0:5253`.
2. Conecta la PC y el Android a la misma red Wi-Fi, sin aislamiento entre dispositivos.
3. Desde el navegador del celular, comprueba que puedes abrir `http://IP-DE-TU-PC:5253/swagger`.
4. Copia el APK al celular, ábrelo e instala la aplicación. Si Android lo solicita, concede permiso de instalación a la aplicación con la que abriste el archivo.
5. Abre **Pac-Man Multijugador**, inicia sesión y prueba una sala. El juego se abre en horizontal y usa los controles táctiles existentes.

Si el celular no puede abrir Swagger, comprueba la dirección, el perfil **red-local** y el permiso de entrada del puerto TCP 5253 en el firewall para esa red local. Instalar el APK no inicia el servidor de C#.

El permiso HTTP (`android/app/src/debug/AndroidManifest.xml`) y el contenido mixto (`MainActivity`) están habilitados solamente en la compilación Android de depuración para esta prueba local: sin ellos Android 9+ bloquea la conexión con la API aunque Swagger abra en el navegador del celular. Desde Android Studio usa la variante **debug**. Para distribuir una versión final, configura una API HTTPS accesible y una firma de publicación propia.

## Android Studio

`npm run android:open` abre el proyecto nativo de `Pacman/android`. Después de cambiar código web, vuelve a ejecutar `npm run build -- --mode android` y `npm run android:sync` antes de generar el APK en Android Studio.

Documentación oficial: [Android en Capacitor](https://capacitorjs.com/docs/android) y [configuración de Capacitor](https://capacitorjs.com/docs/config).
