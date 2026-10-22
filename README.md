# Pacman

## Niveles y carga del laberinto

Los cinco mapas estáticos están en `Base De Datos/06_DatosCatalogos.sql`, dentro de
`laberinto_json`. La matriz de cada nivel es la única fuente de geometría y bolas;
no hay generación aleatoria al cargar una partida.

| Número | Nombre | Diseño | Bolas normales | Bolas de poder |
| --- | --- | --- | ---: | ---: |
| 1 | Laberinto clásico | Sectores simétricos y circuito central con dos accesos | 281 | 4 |
| 2 | Doble anillo | Dos anillos alrededor de un circuito interior, con enlaces radiales | 275 | 4 |
| 3 | Cuatro patios | Cuatro sectores unidos por pasos centrales y laterales | 313 | 4 |
| 4 | Pasajes alternos | Circuitos desplazados con cruces a diferentes alturas | 217 | 4 |
| 5 | Encrucijada | Dos ejes verticales y pasos horizontales que conectan los extremos | 287 | 4 |

Todos tienen 31 filas, 28 columnas, seis puntos de reaparición distribuidos por el
mapa y cinco posiciones iniciales distintas. Los Pac-Man empiezan en la zona
inferior y los fantasmas más arriba, separados por recorridos transitables. No hay
túneles ni teletransporte; todas las bolas se alcanzan con movimientos cardinales.
Los niveles nuevos tienen `velocidad_base = 1.00`; el número no define una curva de
dificultad ni modifica las velocidades ya guardadas.

### Contrato JSON, versión 2

Las propiedades son obligatorias y distinguen mayúsculas de minúsculas. No se
permiten propiedades desconocidas o repetidas, comentarios ni comas finales.
El SQL contiene los cinco documentos completos, con una fila de texto por línea.
La versión 2 agrega `puntosCajas`; los documentos de la versión 1 se rechazan.

| Propiedad | Contenido |
| --- | --- |
| `version` | Entero `2`. Otras versiones se rechazan. |
| `filas`, `columnas` | Enteros positivos que deben coincidir exactamente con la matriz. |
| `casillas` | Arreglo de cadenas; cada cadena representa una fila completa, incluidos los muros. |
| `puntosReaparicion` | Seis objetos distintos con `fila` y `columna`, enteros desde cero. |
| `puntosCajas` | Al menos un objeto con `fila` y `columna`, sin repetir: donde aparecen las cajas de objetos. **Provisionales** (4 por mapa) hasta que el equipo los defina. |
| `posicionesIniciales` | Cinco objetos con `idPersonaje`, `fila` y `columna`; debe aparecer una vez cada personaje 1–5, sin compartir casilla. |

Leyenda de `casillas`:

- `#`: muro, no transitable.
- `.`: casilla transitable con una bola normal (`Punto`).
- `o`: casilla transitable con una bola de poder (`PuntoPoder`).
- Un espacio ASCII: suelo transitable sin bola. Los espacios se conservan; no se recortan las filas.

La matriz ya expresa las posiciones de las bolas; no se repiten en otro arreglo.
Las coordenadas de apariciones y reapariciones deben referirse a suelo transitable.
Los personajes 1 y 2 son Pac-Man; 3, 4 y 5 son fantasmas. El sorteo de roles decide
qué personajes controla la IA, sin cambiar las posiciones del mapa. Contra la IA
solo se usa el Pac-Man 1.

El convertidor rechaza geometría desconectada, ausencia de bolas normales o de
poder, coordenadas inválidas y el formato anterior con `casillas: []`. Las bolas de
poder cuentan para completar el mapa; su efecto sobre los fantasmas corresponde
a los sistemas de juego futuros.

### Uso desde la lógica de negocios

`SistemaNiveles` está en `PacmanLogicaNegocios/Juego/Sistemas/SistemaNiveles.cs`.
No consulta la base de datos: recibe el `TNivel` obtenido mediante `INivelLN`.

```csharp
using PacmanDominio.EntidadesTipadas;
using PacmanLogicaNegocios.Juego.Sistemas;

// nivelCatalogo es el TNivel obtenido del catálogo.
TNivel nivelCatalogo = respuestaNivel.Data;
var sistemaNiveles = new SistemaNiveles();
var nivelCargado = sistemaNiveles.Cargar(nivelCatalogo);
var laberinto = nivelCargado.Laberinto;
var inicioPacManAmarillo = nivelCargado.PosicionesIniciales[1];
```

`Cargar(TNivel)` conserva por valor `IdNivel`, `Numero`, `Nombre` y `VelocidadBase`.
También se puede usar `Convertir(string laberintoJson)`; devuelve el mismo tipo
`NivelCargado`, con esos metadatos de catálogo en `null`. Ambos métodos crean un
`Laberinto`, sus casillas y sus bolas de nuevo en cada llamada. No se debe compartir
un `NivelCargado` entre partidas: debe cargarse uno por sala.

Los errores de contenido JSON se indican con `FormatException` y un mensaje que
identifica el campo o la coordenada. Las entradas nulas, vacías o los metadatos de
catálogo inválidos producen excepciones de argumento. No hay correcciones silenciosas.

### Coordenadas y sprites

La fila crece hacia abajo y la columna hacia la derecha, ambas desde cero. Con
casillas de 16 píxeles, el centro de una posición se calcula así:

```text
x = origenX + columna * 16 + 8
y = origenY + fila * 16 + 8
```

La superficie lógica de estos mapas mide 448 × 496 píxeles antes de escalarla.
`CatalogoSprites.ts` define las casillas e ítems de 16 píxeles, los actores de
32 píxeles y los cuadros 8/9 para bola normal/de poder. El tamaño gráfico del actor
no determina la colisión; su escala y ajuste visual corresponden a la presentación.

`laberinto-clasico.png` es una referencia artística con márgenes y zona superior;
estos datos no prometen correspondencia píxel a píxel con esa imagen. La geometría
debe dibujarse por casillas usando `tiles.png`, también para las otras topologías.
Una imagen de fondo no define colisiones. La galería `?sprites` muestra recursos,
pero todavía no carga estos niveles ni conecta los mapas con una partida.

### Aplicación del SQL

Para una base nueva, ejecutar los scripts de `Base De Datos` en orden; el script 06
inserta los cinco niveles. Los scripts no modifican tablas que ya existen: si la
estructura cambió (como al pasar de frutas a objetos y rondas), ejecutar antes
`00_BorrarBaseDatos.sql`, que borra la base completa, y luego del 01 al 07.
Comprobar primero su `USE PacmanDB`; la carga desde C# no ejecuta estos scripts.

El parche de niveles se ejecuta en una transacción y se puede repetir:

- Inserta únicamente los números 1–5 que falten.
- Repara el nivel 1 solo si su JSON contiene exactamente `filas: 31`, `columnas: 28`
  y `casillas: []`, sin otros campos. El orden de propiedades y los espacios no importan.
- La reparación conserva `id_nivel`, nombre y velocidad, incluyendo sus referencias
  desde partidas existentes. Nunca reemplaza un laberinto personalizado válido.
- Los demás JSON incompletos requieren revisión explícita; no se sustituyen por
  aproximación. Los otros catálogos mantienen su inserción de filas faltantes.

El movimiento, la IA, los efectos, la reaparición segura y el fin de cada nivel están
en el motor del juego (sección siguiente).


## Motor del juego

El motor está en `PacmanLogicaNegocios/Juego`. Solo hace falta usar `MotorPartida`;
los sistemas (`Sistemas/`), la IA (`IA/`, `Controladores/`) y los efectos (`Efectos/`)
son internos. Simula un nivel en ticks fijos de 50 ms y es determinista si se le pasa
una semilla (útil para pruebas).

### Reglas implementadas

- **Movimiento por casillas.** Cada personaje acumula avance según su velocidad y pasa
  a la siguiente casilla al completar 1. Solo gira al llegar a una casilla; la última
  dirección recibida se guarda (como en el clásico, gira en cuanto puede).
- **Velocidad.** Pac-Man 6 casillas/s (87 % mientras come) y fantasmas 5.6; la
  dificultad (`multiplicador_velocidad_ia`) solo cambia a los fantasmas que mueve la
  IA; asustados van a la mitad; aturdidos o congelados no se mueven.
- **Pac-Man contra fantasma.** El fantasma lo atrapa (+300) y Pac-Man pierde una vida.
  Con el fantasma asustado, Pac-Man se lo come (+200) y el fantasma pierde una vida.
  Nadie pierde vidas mientras es invencible (3 s tras reaparecer) o tiene escudo.
- **Reaparición.** A los 2 s, en el punto de reaparición libre más alejado de los rivales.
- **Explosión entre fantasmas.** Quedan aturdidos 2 s y vuelven a puntos de
  reaparición distintos, sin perder vidas.
- **Rebote entre Pac-Man.** Si la sala lo tiene activado, regresan a su casilla y dan
  la vuelta; si no, se atraviesan.
- **Bolas.** Normal +10, de poder +50: asusta a los fantasmas durante
  `duracion_asustado_seg` de la dificultad.
- **Cajas y objetos.** Aparecen en los `puntosCajas` según la frecuencia de la sala
  (baja 20 s, normal 12 s, alta 6 s). Quien pisa una con el espacio libre recibe un
  objeto al azar de su bando; se usa con el botón de acción.
- **Fin del nivel.** Cuando un bando gana; cada integrante del bando ganador recibe +500.
- **IA.** Fantasmas clásicos (Blinky persigue, Pinky embosca 4 casillas adelante, Inky
  encierra con Blinky) que alternan dispersión y persecución y evitan chocar entre sí.
  La IA de Pac-Man come bolas, huye de los fantasmas cercanos y persigue a los
  asustados. La IA usa su objeto cuando tiene un rival a 6 casillas o menos. También
  mueve a los humanos desconectados o que se fueron (`Entidad.ControlIa`).

Todos los valores están en `ReglasJuego.cs` para ajustarlos sin tocar la lógica.

### Objetos

| Objeto | Bando | Efecto | Clase |
| --- | --- | --- | --- |
| Turbo | Pac-Man | `VELOCIDAD`: quien lo usa va 1.5 veces más rápido (5 s) | `EfectoVelocidad` |
| Escudo | Pac-Man | `ESCUDO`: no puede ser atrapado ni caer en trampas (4 s) | `EfectoEscudo` |
| Hielo | Pac-Man | `CONGELAR`: todos los fantasmas quedan inmóviles (3 s) | `EfectoCongelar` |
| Puntos dobles | Pac-Man | `PUNTOS_DOBLES`: gana el doble por bolas y fantasmas (8 s) | `EfectoPuntosDobles` |
| Ralentizar | Fantasma | `RALENTIZAR`: los Pac-Man van a la mitad de velocidad (4 s) | `EfectoRalentizar` |
| Teletransporte | Fantasma | `TELETRANSPORTE`: salta hasta 6 casillas hacia adelante, sin atravesar muros | `EfectoTeletransporte` |
| Trampa de hielo | Fantasma | `TRAMPA`: deja una trampa en su casilla (15 s); el Pac-Man que la pisa queda congelado 3 s | `EfectoTrampa` |

Para agregar un objeto: una clase que implemente `IEfectoObjeto` (solo los métodos
que use: `MultiplicadorVelocidad`, `MultiplicadorPuntos`, `Protege`, `Aplicar`, `Retirar`), agregarla a
`MotorPartida.EfectosPredeterminados` y agregar su fila a `objetos` con la clave en
`efecto`. Los objetos cuyo efecto no tiene clase se ignoran.

### Uso desde el gestor de salas 

El motor no es seguro para varios hilos: todas las llamadas de una sala deben ir
serializadas (por ejemplo, con el mismo candado o canal que protege la `Sala`).

1. **Iniciar la serie.** Con `INivelLN` y `SistemaNiveles.Cargar` se carga el nivel 1.
   `AsignadorPersonajes` sortea los roles:
   - con 1 jugador el modo es contra la IA: 1 Pac-Man (el personaje 1) y los 3
     fantasmas; el jugador toma su `RolPreferido` o uno al azar;
   - con 2 a 4 jugadores, los humanos se sortean entre los personajes 1 a 4 y el resto
     (siempre incluido el 5) es IA.

   Con eso se construye el `EstadoPartida` (las vidas salen de `sala.Configuracion`) y
   se llama a `sala.IniciarPartida`. Después: `PartidaLN.IniciarAsync`,
   `sala.RegistrarIdPartida`,
   `new MotorPartida(nivel, dificultad, tNivel.VelocidadBase, objetos)` y
   `NotificarNivelAsync(NivelDto.Crear(nivel))`.
2. **Cada tick** (un `PeriodicTimer` de 50 ms basta):
   `var eventos = motor.Avanzar(transcurrido);` y
   `NotificarEstadoAsync(EstadoPartidaDto.Crear(nivel, sala, eventos))`.
3. **Controles.** `EnviarDireccionAsync` → `motor.RecibirDireccion(idPersonaje, dir)`;
   `UsarObjetoAsync` → `motor.SolicitarUsarObjeto(idPersonaje)`. El personaje del
   usuario sale de `nivel.ObtenerActorDeUsuario(idUsuario)`.
4. **Fin del nivel** (`motor.NivelTerminado`):
   `PartidaLN.GuardarRondaAsync(motor.CrearRonda(...), motor.ObtenerParticipantes())`
   y `sala.TerminarNivel()`.
   - Si la sala queda en `Transicion`: esperar `ReglasJuego.DuracionTransicion`,
     sortear roles de nuevo (contra la IA se conserva `sala.RolContraIa`), llamar a
     `sala.IniciarSiguienteNivel` y crear un motor nuevo.
   - Si queda `Finalizada`: `PartidaLN.FinalizarAsync` con `FINALIZADA`.
5. **Desconexiones.** `sala.Desconectar`, `Reconectar` y `Salir` ya ceden o devuelven
   el control a la IA; el motor lo detecta solo. Si todos se van, la sala queda
   `Finalizada`: guardar la ronda en curso (queda sin ganador) y cerrar la partida
   como `ABANDONADA`.

Para el cliente, `EstadoPartidaDto` trae `Direccion` y `Avance` de cada personaje
(para interpolar entre ticks) y `Eventos` (`TipoEventoJuego`) para sonidos y
animaciones: bola comida, fantasma comido, Pac-Man atrapado, explosión, rebote,
reaparición, caja, objeto usado y fin del nivel.

## Gestor de salas

`GestorSalas` implementa `IGestorSalas` en
`PacmanLogicaNegocios/Juego/Salas/`. Se comparte durante la vida del servidor:
conserva hasta cinco salas en memoria y reserva cada usuario y conexión en una
sola sala. Una desconexión conserva la plaza; una salida voluntaria la libera.
La conexión anterior deja de servir después de reconectar.

El primer jugador es el anfitrión. En el lobby puede configurar la dificultad,
vidas, rebote y frecuencia de cajas. Cada jugador puede elegir su preferencia
para jugar solo. `AsignadorPersonajes` aplica los sorteos de la sección anterior,
rellena con IA y vuelve a sortear al cambiar de nivel. Si un multijugador queda
con un solo humano, conserva el modo multijugador; contra IA se conserva el bando.

El nivel se busca por `Numero` en `INivelLN.ListarAsync`; nunca se supone que
`IdNivel` coincide con ese número. El convertidor requiere los JSON versión 2 del
catálogo actual. Cada nivel de cada sala tiene casillas, bolas, actores, dificultad,
objetos y motor propios; las vidas salen de `Sala.Configuracion`.

### Inicio y persistencia

El gestor carga y valida los datos y prepara un motor sobre una copia del lobby.
Esa copia usa las operaciones de `Sala` para aceptar el primer nivel. Solo se
publica como sala en juego después de que `IPartidaLN.IniciarAsync` confirma el
identificador y `RegistrarIdPartida` lo acepta. Así un fallo de carga o de guardado
no deja visible una partida sin motor o sin identificador.

Si el inicio queda pendiente, se conservan el sorteo y la fecha de inicio. Hasta
confirmarlo no se admiten ingresos ni cambios de configuración o preferencia; las
salidas, desconexiones y reconexiones siguen aplicándose también a la copia
preparada. Incluso si todos salen durante un inicio de resultado incierto, el
gestor recupera su identificador, guarda la interrupción y cierra como `ABANDONADA`.

Al terminar un nivel, primero se guarda la ronda con sus participantes. Solo al
confirmarla se llama a `TerminarNivel`; los cinco segundos de transición empiezan
entonces. Si falta el próximo nivel o falla su carga, la sala permanece en transición.
Los guardados fallidos se reintentan una vez por segundo sin cambiar sus fechas o
claves. Un reintento confirmado no vuelve a guardar rondas anteriores ni crea otra
serie. Terminar el nivel cinco determina `FINALIZADA`, incluso si el último humano
sale mientras todavía se reintenta guardar ese resultado.

Si todos salen antes de completar la serie, se guarda el nivel en curso sin ganador
y se cierra como `ABANDONADA`. Durante una transición se conserva la ronda ya
terminada, sin inventar otra. Una sala vacía de lobby se retira inmediatamente;
una sala vacía con resultados pendientes se retira después de confirmarlos. Las
salas finalizadas admiten salidas y conservan su capacidad hasta quedar vacías.
No hay expiración automática de jugadores desconectados.

### Ciclo y concurrencia

Al crear cada sala empieza una tarea propia con `PeriodicTimer` de 50 ms. El tiempo
transcurrido procede de `TimeProvider.GetTimestamp`, y el motor lo descompone en
sus ticks fijos. `TimeProvider` es inyectable para pruebas; por defecto es el del
sistema. No hace falta que el adaptador de red llame al motor ni que avance el reloj.

Todas las operaciones de una sala, sus controles, cargas, guardados y notificaciones
pasan por la misma compuerta. El candado de índices globales solo cubre operaciones
breves y nunca espera una compuerta ni servicios externos. Una consulta, guardado
o notificación lenta retiene su sala, pero las otras siguen ejecutándose.

Se envían instantáneas `SalaDto`, `NivelDto` y `EstadoPartidaDto`. Los fallos de
notificación se registran y se reintentan con los indicadores pendientes; no
deshacen el juego ni repiten la persistencia. El ciclo permanece mientras exista
la sala, también después de finalizar la serie, para reenviar avisos finales y
cambios de jugadores. En esa fase no se avanza el motor ni se repiten guardados.
Al retirar la sala se dispone su temporizador y termina su tarea.

Al reconectar se envían sala, mapa y estado a la conexión nueva. También se pueden
consultar con `ObtenerAsync`, `ObtenerNivelAsync` y `ObtenerEstadoAsync`. Los eventos
son avisos del transporte, sin acuse de recibo ni garantía de entrega exactamente
una vez; un cliente debe poder recuperar su estado mediante las instantáneas.

`DisposeAsync` cierra la admisión, cancela las esperas, dispone los temporizadores,
espera las tareas y las operaciones activas y guarda las series interrumpidas como
abandonadas. El cierre es idempotente; si no puede confirmar algún resultado, termina
sus recursos y devuelve `AggregateException`, además de registrarlo. Los contratos
de datos/notificación no reciben cancelación: una llamada ya iniciada debe terminar
para poder liberar su ámbito con seguridad. Los reintentos y las salas son memoria
del proceso; no hay recuperación automática tras un cierre abrupto del servidor.

### Dependencias y composición

El constructor recibe `IServiceScopeFactory`, `INotificadorJuego`,
`ILogger<GestorSalas>` y, opcionalmente, `TimeProvider`. Cada operación de catálogos
o persistencia abre y dispone un ámbito corto. Los servicios `INivelLN`,
`IDificultadLN`, `IObjetoLN` e `IPartidaLN` conservan su registro `scoped`;
el gestor no captura servicios de datos ni comparte un `DbContext` entre salas.

En `PacmanApi/Program.cs` el gestor y el notificador (`Hubs/NotificadorJuego.cs`,
sobre `IHubContext`) se registran como **singleton**: como `scoped` o `transient`
cada solicitud obtendría salas distintas. `Servicios/ParadaGestorSalas.cs` cierra
el gestor en `IHostedService.StopAsync`, **antes** de disponer el proveedor DI,
porque después ya no se pueden crear los ámbitos que guardan las series
interrumpidas.

## Tiempo real (SignalR)

El hub está en `/hubs/juego` (`PacmanApi/Hubs/JuegoHub.cs`). Solo recibe intenciones
(dirección y botón de acción) con la identidad ya validada; nunca posiciones ni
estados. Cada método responde con el mismo `Respuesta<T>` de la API REST
(`success`, `data`, `error`). Los JSON van en camelCase y los enums como número.

### Conexión

Se necesita el token de `api/Usuario/IniciarSesion`. Con `@microsoft/signalr`:

```ts
const conexion = new signalR.HubConnectionBuilder()
    .withUrl(`${servidor}/hubs/juego`, { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .build();
```

En WebSockets el navegador no permite encabezados, así que el cliente envía el token
como `?access_token=`. `Seguridad/AutenticacionSesion.cs` lo acepta solo en `/hubs`.
Sin token o con uno vencido, la conexión responde 401.

### Métodos (cliente → servidor)

| Método | Parámetros | Respuesta | Notas |
| --- | --- | --- | --- |
| `ListarSalas` | — | `SalaDto[]` | Para la lista de salas |
| `CrearSala` | — | `SalaDto` | Quien la crea es el anfitrión. Máximo 5 salas |
| `UnirseSala` | `codigo` | `SalaDto` | Solo en el lobby, máximo 4 jugadores |
| `SalirSala` | `codigo` | `SalaDto` | Libera la plaza. Usarlo también al terminar la serie |
| `ReconectarSala` | `codigo` | `SalaDto` | Con una conexión nueva; envía al instante sala, mapa y estado |
| `ReanudarSala` | — | `SalaDto` | Igual, si el cliente ya no recuerda el código |
| `ConfigurarSala` | `codigo`, `ConfiguracionSalaDto` | `SalaDto` | Solo el anfitrión, en el lobby |
| `ElegirRol` | `codigo`, `"PACMAN"`, `"FANTASMA"` o `null` | `SalaDto` | Solo cuenta si juega solo contra la IA |
| `IniciarPartida` | `codigo` | `SalaDto` | Solo el anfitrión |
| `EnviarDireccion` | `codigo`, `direccion` (1 arriba, 2 abajo, 3 izquierda, 4 derecha) | `bool` | Se guarda hasta que pueda girar |
| `UsarObjeto` | `codigo` | `bool` | Botón de acción |
| `ObtenerSala`, `ObtenerNivel`, `ObtenerEstado` | `codigo` | `SalaDto`, `NivelDto`, `EstadoPartidaDto` | Para recuperarse si se perdió un aviso |

### Avisos (servidor → cliente)

| Evento | Datos | Cuándo |
| --- | --- | --- |
| `SalaActualizada` | `SalaDto` | Cambios de jugadores, configuración o estado (`estado`: 1 lobby, 2 jugando, 3 transición, 4 finalizada) |
| `NivelIniciado` | `NivelDto` | Al empezar cada nivel: el mapa (filas de texto), las bolas y los puntos de cajas |
| `EstadoActualizado` | `EstadoPartidaDto` | Unas 20 veces por segundo: personajes (con `direccion` y `avance` para interpolar), bolas comidas, cajas, efectos y `eventos` |

Los avisos no garantizan entrega exactamente una vez. Si el cliente se reconecta,
debe llamar a `ReconectarSala` o `ReanudarSala` y continuar con las instantáneas que
recibe. Si pierde la señal, conserva su plaza y la IA mueve su personaje hasta que
vuelva; si sale con `SalirSala`, la IA termina el nivel por él.

### CORS y pruebas con celulares

El cliente de SignalR envía credenciales, por eso los orígenes deben ser
explícitos. En desarrollo se acepta cualquier origen. Fuera de desarrollo se usan
los de `Cors:Origenes` en `appsettings.json`: el servidor de Vite
(`http://localhost:8080`) y Capacitor (`https://localhost`, `http://localhost`,
`capacitor://localhost`).

El perfil `http` solo escucha en `localhost`. Para conectar un celular de la misma
red, arrancar con el perfil `red-local` (`dotnet run --launch-profile red-local`),
que escucha en `http://0.0.0.0:5253`, y usar la IP de la computadora como servidor.
Puede que Windows pida permitir el acceso en el firewall.
