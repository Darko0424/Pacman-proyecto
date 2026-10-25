# Adaptación de pantalla móvil

## Causas y comportamiento nuevo

El lienzo fijo 1366 × 768 con `Scale.FIT` dejaba franjas en pantallas de otras
proporciones. Además, `100vh` y los insets automáticos de Capacitor permitían que
el teclado redujera la WebView y Phaser escalara toda la interfaz hacia abajo.
El campo de contraseña también usaba una clase con ñ que no coincidía con su CSS.

- El lienzo ahora ajusta su proporción al ancho útil, conservando escala uniforme.
  La interfaz mantiene una referencia de 1024 × 768 y la partida distribuye sus
  paneles según el espacio disponible. Las cámaras se recentran sin reiniciar escenas.
- Formularios y menús caben siempre enteros en la pantalla, sin barra de
  desplazamiento (antes la escala no bajaba de 0.75 y en teléfonos bajos aparecía
  el scroll). En pantallas muy bajas la interfaz se ve más pequeña. Los campos tienen al menos 16 px CSS de texto
  y 44 px CSS de alto. Al abrir el teclado se conserva la escala y se desplaza el
  campo activo al área visible. Los mismos nodos HTML conservan su valor y foco.
- El estado de sesión, las peticiones pendientes y la interpolación de la partida
  permanecen en sus objetos originales. Al redimensionar la partida solo se vuelven
  a construir sus vistas; no se pide reiniciar el nivel ni se cambian reglas.
- Las zonas de cámara y navegación se descuentan mediante insets. Android usa
  barras ocultas con reaparición mediante gesto y `adjustNothing`. Se desactiva
  `SystemBars.insetsHandling` para que haya un solo propietario de insets; el
  listener de `MainActivity` comunica áreas seguras e IME a la web, sin añadir padding
  de teclado a la WebView. Si el sistema también la reduce, no se resta el IME dos veces.
- Los botones ignoran desplazamientos y eventos táctiles cancelados. La cruceta y
  el botón SALIR se dimensionan para el uso táctil en horizontal. Los sprites y las
  casillas mantienen su proporción. Las sugerencias de nombre y los avisos se ajustan
  al ancho disponible sin vaciar los campos ni repetir consultas.

## Archivos

| Archivos | Responsabilidad |
| --- | --- |
| `src/presentacion/ui/PantallaAdaptable.ts` | Área visible, lienzo, teclado, desplazamiento y ciclo de eventos. |
| `src/presentacion/ui/DistribucionJuego.ts` | Geometría de los paneles y del tablero. |
| `src/game/main.ts`, `index.html`, `public/style.css` | Arranque, viewport, áreas seguras y tamaño físico de campos. |
| `ui/CampoTexto.ts`, `ui/Boton.ts`, `ui/Panel.ts` | Campos persistentes, cancelación táctil y marco adaptable. |
| `ui/HUD.ts`, `ui/ControlesTactiles.ts`, `escenas/EscenaJuego.ts` | Controles, paneles y vistas del nivel al cambiar de tamaño. |
| `escenas/FlujoSesion.ts` y escenas Login, Registro, Verificacion, Nombre, Menu, Ranking, Historial, ListaSalas y Sala | Referencia visual común y conservación de datos al redimensionar. |
| `capacitor.config.json`, `android/app/src/main/AndroidManifest.xml`, `MainActivity.java` | Pantalla completa e integración nativa de insets y teclado. |
| `android/app/build.gradle` | Actualización instalable: versionCode 2, versionName 1.0.1. |

Las rutas `ui/` y `escenas/` de la tabla parten de `src/presentacion/`.

## Verificación y límites

Se comprueban TypeScript y la compilación web. El arnés externo verifica geometría
en 640×300, 640×360, 800×320, 844×390, 947×396, 1024×768, 1280×720, 1366×768 y 1920×1080;
también comprueba que en 390×844 no haya dimensiones negativas ni recortes de zonas.
Simula apertura/cierre de teclado nativo y de navegador, redimensionado adicional,
conservación de escala/texto, limpieza de listeners y cancelación de botones. Son
pruebas de componentes; no sustituyen la comprobación del WebView y Gboard reales.

El diseño de la partida y la orientación Android siguen siendo horizontales. La
comprobación vertical solo verifica contención: no garantiza el tamaño táctil de
la cruceta en vertical. En teléfonos bajos, formularios y menús se reducen para
caber; solo con el teclado abierto se desplaza el contenido hasta el campo activo. El sistema puede mostrar temporalmente sus barras o una pantalla
especial de edición de su teclado. La validación del APK final, dispositivos con
recortes físicos y otras versiones de Android/WebView corresponde a la prueba nativa;
no se promete compatibilidad universal a partir de esta matriz.

Esta tarea no modifica SQL, configuración de la API, credenciales, reglas del juego
ni `descargas.zip`. La compilación y prueba finales del APK se realizan después de
sincronizar Capacitor con estas fuentes; no se genera ni publica una versión release.

## APK 1.0.1: validacion nativa completada

El 24 de octubre de 2026 se compilo e instalo la actualizacion en un emulador Android
con WebView y Gboard reales. TypeScript, Vite y Gradle completaron la compilacion.

- En 640 x 360: contrasena de login y ultimo campo de registro visibles sobre el teclado,
  con altura de aproximadamente 44 px CSS y escala constante. Se repitio el registro
  despues de navegar y redimensionar; conserva el texto.
- En 844 x 390: login con teclado abierto y cerrado, escala y texto conservados.
- Deslizar sobre CREAR CUENTA no navega; un toque si navega. VER/OCULTAR, Enter
  y redimensionar conservando los mismos objetos de escena y campo pasaron.
- Se inspeccionaron tablero, controles y sugerencias a 640 x 360, el tablero a
  844 x 390 y 640 x 300 con cuatro efectos, y vistas en pantalla grande.

La prueba detecto y corrigio un caso donde el ultimo campo quedaba oculto:
el contenedor conservaba el limite de desplazamiento anterior al teclado.
Ahora se mide el rectangulo real despues del render y se desplaza solo #app;
un gesto manual permite volver a desplazar el contenido.

Salida local: [PacmanMultijugador-1.0.1.apk](descargas/PacmanMultijugador-1.0.1.apk).
Evidencia resumida: [prueba-pantalla-android.json](descargas/prueba-pantalla-android.json).
SHA-256: 93409FC0372714B669DD68F5956278C9756EB51BB37F5F493F1EC38F4D66EC89.

Instalar este APK sobre el anterior conserva los datos de la aplicacion. Para conectarse,
mantener PacmanApi iniciado con red-local y el celular en la misma Wi-Fi.
La direccion incluida es http://10.75.181.202:5253.
El ZIP anterior no se actualizo: si se comparte un ZIP, debe contener este APK nuevo.

Estas pruebas no sustituyen una partida entre cuatro celulares fisicos. La partida
esta disenada en horizontal.
