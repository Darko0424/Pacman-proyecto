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

### Objetos (provisionales)

| Objeto | Bando | Efecto | Clase |
| --- | --- | --- | --- |
| Turbo | Pac-Man | `VELOCIDAD`: quien lo usa va 1.5 veces más rápido | `EfectoVelocidad` |
| Escudo | Pac-Man | `ESCUDO`: no puede ser atrapado | `EfectoEscudo` |
| Impulso | Fantasma | `VELOCIDAD` | `EfectoVelocidad` |
| Hielo | Fantasma | `CONGELAR`: el bando rival queda inmóvil | `EfectoCongelar` |

Para agregar un objeto: una clase que implemente `IEfectoObjeto` (solo los métodos
que use: `MultiplicadorVelocidad`, `Protege`, `Aplicar`, `Retirar`), agregarla a
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
