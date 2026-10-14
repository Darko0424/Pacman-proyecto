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

### Contrato JSON, versión 1

Las propiedades son obligatorias y distinguen mayúsculas de minúsculas. No se
permiten propiedades desconocidas o repetidas, comentarios ni comas finales.
El SQL contiene los cinco documentos completos, con una fila de texto por línea.

| Propiedad | Contenido |
| --- | --- |
| `version` | Entero `1`. Otras versiones se rechazan. |
| `filas`, `columnas` | Enteros positivos que deben coincidir exactamente con la matriz. |
| `casillas` | Arreglo de cadenas; cada cadena representa una fila completa, incluidos los muros. |
| `puntosReaparicion` | Seis objetos distintos con `fila` y `columna`, enteros desde cero. |
| `posicionesIniciales` | Cinco objetos con `idPersonaje`, `fila` y `columna`; debe aparecer una vez cada personaje 1–5, sin compartir casilla. |

Leyenda de `casillas`:

- `#`: muro, no transitable.
- `.`: casilla transitable con una bola normal (`Punto`).
- `o`: casilla transitable con una bola de poder (`PuntoPoder`).
- Un espacio ASCII: suelo transitable sin bola. Los espacios se conservan; no se recortan las filas.

La matriz ya expresa las posiciones de las bolas; no se repiten en otro arreglo.
Las coordenadas de apariciones y reapariciones deben referirse a suelo transitable.
Los personajes 1 y 2 son Pac-Man; 3, 4 y 5 son fantasmas. La sala decide cuál de
estos tres fantasmas controla el bot, sin cambiar las posiciones del mapa.

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
inserta los cinco niveles. En una base que ya tiene las tablas, volver a ejecutar
`06_DatosCatalogos.sql` en la instancia y base de destino elegidas por el equipo.
Comprobar primero su `USE PacmanDB`; la carga desde C# no ejecuta este script.

El parche de niveles se ejecuta en una transacción y se puede repetir:

- Inserta únicamente los números 1–5 que falten.
- Repara el nivel 1 solo si su JSON contiene exactamente `filas: 31`, `columnas: 28`
  y `casillas: []`, sin otros campos. El orden de propiedades y los espacios no importan.
- La reparación conserva `id_nivel`, nombre y velocidad, incluyendo sus referencias
  desde partidas existentes. Nunca reemplaza un laberinto personalizado válido.
- Los demás JSON incompletos requieren revisión explícita; no se sustituyen por
  aproximación. Los otros catálogos mantienen su inserción de filas faltantes.

Quedan fuera de esta carga el motor, movimiento, IA, efectos, selección segura de
reaparición y avance automático entre niveles. La ubicación inicial separada y los
seis candidatos de reaparición no sustituyen esas reglas de ejecución.
