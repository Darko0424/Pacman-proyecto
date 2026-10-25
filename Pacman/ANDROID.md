# Probar Pac-Man en Android

El cliente Phaser se empaqueta con Capacitor. La API de C# y SQL Server siguen ejecutándose en la PC.

## Generar el APK de prueba

Requisitos: Node.js 22 o posterior, las dependencias de `npm ci`, Android SDK Platform 36 y un JDK entre 21 y 24 compatible con Gradle 8.14.3.

Desde la carpeta `Pacman`:

```powershell
npm run android:apk -- -ServidorApi http://IP-DE-TU-PC:5253
```

Sustituye `IP-DE-TU-PC` por la IPv4 del adaptador Wi-Fi; puedes verla con `ipconfig`. El celular debe poder acceder a esa misma PC. `localhost` en el celular apunta al propio teléfono.

Como alternativa, copia `.env.android.example` a `.env.android.local`, escribe allí la URL y ejecuta `npm run android:apk`. Este archivo local está ignorado por Git.

El asistente verifica TypeScript, compila el cliente en modo Android, sincroniza Capacitor y genera un APK firmado con la clave de depuración. El resultado queda en:

```text
Pacman/descargas/PacmanMultijugador-debug.apk
```

La carpeta `descargas` también contiene `prueba-android.json` con la dirección incluida, la fecha y la huella SHA-256. No se incluyen contraseñas de SQL ni del correo en el APK.

La dirección que se indica al compilar es solo la predeterminada. Si cambian de red o de PC no hace falta otro APK. Con `PacmanApi` iniciado con el perfil **red-local**, se abren Swagger y la página del QR en el navegador de la PC. Si la segunda pestaña no aparece, abre `http://localhost:5253/conectar`. En la app toca **SERVIDOR → ESCANEAR QR** y concede permiso de cámara. La app comprueba que la API y la base respondan, guarda la dirección y vuelve al inicio de sesión. El QR solo contiene la dirección local de la API.

También puedes escribir la IP en **SERVIDOR** (por ejemplo `192.168.1.100`; el puerto 5253 se agrega solo) y pulsar **PROBAR Y GUARDAR**. Esta opción funciona si la cámara no está disponible. **PREDETERMINADA** vuelve a la dirección incluida al compilar. La compilación del navegador conserva su configuración habitual.

## Iniciar el servidor y probar

1. En Visual Studio, selecciona `PacmanApi` y el perfil **red-local**. Este perfil escucha en `http://0.0.0.0:5253`.
2. Conecta la PC y el Android a la misma red Wi-Fi, sin aislamiento entre dispositivos.
3. Desde el navegador del celular, comprueba que puedes abrir `http://IP-DE-TU-PC:5253/swagger`.
4. Copia el APK al celular, ábrelo e instala la aplicación. Si Android lo solicita, concede permiso de instalación a la aplicación con la que abriste el archivo.
5. Abre **Pac-Man Multijugador**, inicia sesión y prueba una sala. El juego se abre en horizontal y usa los controles táctiles existentes.

Si el celular no puede abrir Swagger, comprueba la dirección, el perfil **red-local** y el permiso de entrada del puerto TCP 5253 en el firewall para esa red local. Instalar el APK no inicia el servidor de C#.

El permiso HTTP (`android/app/src/debug/AndroidManifest.xml`) y el contenido mixto (`MainActivity`) están habilitados solamente en la compilación Android de depuración para esta prueba local: sin ellos Android 9+ bloquea la conexión con la API aunque Swagger abra en el navegador del celular. Desde Android Studio usa la variante **debug**. Para distribuir una versión final, configura una API HTTPS accesible y una firma de publicación propia.

## Android Studio

`npm run android:open` abre el proyecto nativo de `Pacman/android`. Después de cambiar código web, vuelve a generar el APK con el asistente para incluirlo.

Documentación oficial: [Android en Capacitor](https://capacitorjs.com/docs/android) y [configuración de Capacitor](https://capacitorjs.com/docs/config).
