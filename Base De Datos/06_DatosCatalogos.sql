-- =============================================
-- 06. DATOS DE CATÁLOGOS
-- Solo inserta las filas que falten.
-- =============================================

USE PacmanDB;
GO

INSERT INTO personajes (id_personaje, nombre, rol, color)
SELECT v.id_personaje, v.nombre, v.rol, v.color
FROM (VALUES
    (1, 'Pac-Man 1', 'PACMAN',   'AMARILLO'),
    (2, 'Pac-Man 2', 'PACMAN',   'VERDE'),
    (3, 'Blinky',    'FANTASMA', 'ROJO'),
    (4, 'Pinky',     'FANTASMA', 'ROSA'),
    (5, 'Inky',      'FANTASMA', 'CELESTE')
) AS v (id_personaje, nombre, rol, color)
WHERE NOT EXISTS (SELECT 1 FROM personajes p WHERE p.id_personaje = v.id_personaje);
GO

INSERT INTO dificultades (id_dificultad, nombre, multiplicador_velocidad_ia, duracion_asustado_seg)
SELECT v.id_dificultad, v.nombre, v.multiplicador_velocidad_ia, v.duracion_asustado_seg
FROM (VALUES
    (1, 'FACIL',   0.85, 10),
    (2, 'NORMAL',  1.00, 7),
    (3, 'DIFICIL', 1.20, 4)
) AS v (id_dificultad, nombre, multiplicador_velocidad_ia, duracion_asustado_seg)
WHERE NOT EXISTS (SELECT 1 FROM dificultades d WHERE d.id_dificultad = v.id_dificultad);
GO

INSERT INTO frutas (id_fruta, nombre, efecto, duracion_seg, puntos)
SELECT v.id_fruta, v.nombre, v.efecto, v.duracion_seg, v.puntos
FROM (VALUES
    (1, 'Cereza',  'DOBLE_PUNTAJE',      10, 100),
    (2, 'Fresa',   'VELOCIDAD',          8,  300),
    (3, 'Naranja', 'CONGELAR_FANTASMAS', 5,  500),
    (4, 'Manzana', 'INVISIBILIDAD',      6,  700)
) AS v (id_fruta, nombre, efecto, duracion_seg, puntos)
WHERE NOT EXISTS (SELECT 1 FROM frutas f WHERE f.id_fruta = v.id_fruta);
GO

-- PENDIENTE: faltan los niveles 2 a 5 y las casillas del laberinto del nivel 1
INSERT INTO niveles (numero, nombre, laberinto_json, velocidad_base)
SELECT v.numero, v.nombre, v.laberinto_json, v.velocidad_base
FROM (VALUES
    (1, 'Laberinto clásico', N'{"filas":31,"columnas":28,"casillas":[]}', 1.00)
) AS v (numero, nombre, laberinto_json, velocidad_base)
WHERE NOT EXISTS (SELECT 1 FROM niveles n WHERE n.numero = v.numero);
GO
