-- =============================================
-- 06. DATOS DE CATÁLOGOS
-- Inserta filas faltantes y repara únicamente el placeholder reconocido del nivel 1.
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

-- Cinco niveles estáticos. El contrato JSON versión 1 se documenta en README.md.
-- Los mapas válidos existentes, sus nombres, velocidades e identificadores se conservan.
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @NivelesPredeterminados TABLE (
        numero INT PRIMARY KEY,
        nombre NVARCHAR(50) NOT NULL,
        laberinto_json NVARCHAR(MAX) NOT NULL,
        velocidad_base DECIMAL(4,2) NOT NULL
    );

    INSERT INTO @NivelesPredeterminados (numero, nombre, laberinto_json, velocidad_base)
    VALUES
    (1, N'Laberinto clásico', N'{
    "version": 1,
    "filas": 31,
    "columnas": 28,
    "casillas": [
        "############################",
        "#..........................#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#o........................o#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#..........................#",
        "#.####.##############.####.#",
        "#.####.##############.####.#",
        "#.####.##############.####.#",
        "#.####.### .. ... ###.####.#",
        "#.####.###.######.###.####.#",
        "#..........######..........#",
        "#.####.###.######.###.####.#",
        "#.####.###........###.####.#",
        "#.####.##############.####.#",
        "#.####.##############.####.#",
        "#.####.##############.####.#",
        "#..........................#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#o.... .............. ....o#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#..........................#",
        "############################"
    ],
    "puntosReaparicion": [
        {"fila": 1, "columna": 6},
        {"fila": 1, "columna": 21},
        {"fila": 15, "columna": 1},
        {"fila": 15, "columna": 26},
        {"fila": 29, "columna": 6},
        {"fila": 29, "columna": 21}
    ],
    "posicionesIniciales": [
        {"idPersonaje": 1, "fila": 25, "columna": 6},
        {"idPersonaje": 2, "fila": 25, "columna": 21},
        {"idPersonaje": 3, "fila": 13, "columna": 10},
        {"idPersonaje": 4, "fila": 13, "columna": 13},
        {"idPersonaje": 5, "fila": 13, "columna": 17}
    ]
}', 1.00),
    (2, N'Doble anillo', N'{
    "version": 1,
    "filas": 31,
    "columnas": 28,
    "casillas": [
        "############################",
        "#o........................o#",
        "#.###########.############.#",
        "#.###########.############.#",
        "#.###########.############.#",
        "#..........................#",
        "#.###.#######.########.###.#",
        "#.###.#######.########.###.#",
        "#.###.#######.########.###.#",
        "#.......... . .. ..........#",
        "#.###.###.########.###.###.#",
        "#.###.###.########.###.###.#",
        "#.###.###.########.###.###.#",
        "#.###.###.########.###.###.#",
        "#.###.###.########.###.###.#",
        "#.###..................###.#",
        "#.###.###.########.###.###.#",
        "#.###.###.########.###.###.#",
        "#.###.###.########.###.###.#",
        "#.###.###.########.###.###.#",
        "#.###.###.########.###.###.#",
        "#..........................#",
        "#.###.#######.########.###.#",
        "#.###.#######.########.###.#",
        "#.###.#######.########.###.#",
        "#....o................o....#",
        "#.###########.############.#",
        "#.###########.############.#",
        "#.###########.############.#",
        "#.... ................ ....#",
        "############################"
    ],
    "puntosReaparicion": [
        {"fila": 1, "columna": 7},
        {"fila": 1, "columna": 20},
        {"fila": 15, "columna": 5},
        {"fila": 15, "columna": 22},
        {"fila": 29, "columna": 7},
        {"fila": 29, "columna": 20}
    ],
    "posicionesIniciales": [
        {"idPersonaje": 1, "fila": 29, "columna": 5},
        {"idPersonaje": 2, "fila": 29, "columna": 22},
        {"idPersonaje": 3, "fila": 9, "columna": 11},
        {"idPersonaje": 4, "fila": 9, "columna": 13},
        {"idPersonaje": 5, "fila": 9, "columna": 16}
    ]
}', 1.00),
    (3, N'Cuatro patios', N'{
    "version": 1,
    "filas": 31,
    "columnas": 28,
    "casillas": [
        "############################",
        "#..........................#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#.###o......#.##......o###.#",
        "#.###.#####.#.##.#####.###.#",
        "#.....#####.#.##.#####.....#",
        "#.###.#####.#.##.#####.###.#",
        "#.###.......#.##.......###.#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#.......... . .. ..........#",
        "#.#####.#####.######.#####.#",
        "#..........................#",
        "#.#####.#####.######.#####.#",
        "#..........................#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#.###.......#.##.......###.#",
        "#.###.#####.#.##.#####.###.#",
        "#.....#####.#.##.#####.....#",
        "#.###.#####.#.##.#####.###.#",
        "#.###o......#.##......o###.#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#.#########.#.##.#########.#",
        "#..... .............. .....#",
        "############################"
    ],
    "puntosReaparicion": [
        {"fila": 1, "columna": 6},
        {"fila": 1, "columna": 21},
        {"fila": 15, "columna": 1},
        {"fila": 15, "columna": 26},
        {"fila": 29, "columna": 6},
        {"fila": 29, "columna": 21}
    ],
    "posicionesIniciales": [
        {"idPersonaje": 1, "fila": 29, "columna": 6},
        {"idPersonaje": 2, "fila": 29, "columna": 21},
        {"idPersonaje": 3, "fila": 13, "columna": 11},
        {"idPersonaje": 4, "fila": 13, "columna": 13},
        {"idPersonaje": 5, "fila": 13, "columna": 16}
    ]
}', 1.00),
    (4, N'Pasajes alternos', N'{
    "version": 1,
    "filas": 31,
    "columnas": 28,
    "casillas": [
        "############################",
        "#o.........................#",
        "#.##############.#########.#",
        "#.##############.#########.#",
        "#.##############.#########.#",
        "#.#########...............o#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#..... .... .... #########.#",
        "#.#########.##############.#",
        "#.#########.##############.#",
        "#.#########.##############.#",
        "#.#########................#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#................#########.#",
        "#.##############.#########.#",
        "#.##############.#########.#",
        "#.##############.#########.#",
        "#.#########................#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#o...............#########.#",
        "#.#########.##############.#",
        "#.#########.##############.#",
        "#.#########.##############.#",
        "#..... .............. ....o#",
        "############################"
    ],
    "puntosReaparicion": [
        {"fila": 1, "columna": 6},
        {"fila": 5, "columna": 26},
        {"fila": 13, "columna": 1},
        {"fila": 17, "columna": 26},
        {"fila": 29, "columna": 6},
        {"fila": 29, "columna": 21}
    ],
    "posicionesIniciales": [
        {"idPersonaje": 1, "fila": 29, "columna": 6},
        {"idPersonaje": 2, "fila": 29, "columna": 21},
        {"idPersonaje": 3, "fila": 9, "columna": 6},
        {"idPersonaje": 4, "fila": 9, "columna": 11},
        {"idPersonaje": 5, "fila": 9, "columna": 16}
    ]
}', 1.00),
    (5, N'Encrucijada', N'{
    "version": 1,
    "filas": 31,
    "columnas": 28,
    "casillas": [
        "############################",
        "#..........................#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#....o......####......o....#",
        "#.###.#####.####.#####.###.#",
        "#.###.#####.####.#####.###.#",
        "#.###.#####.####.#####.###.#",
        "#.###...... . .. ......###.#",
        "#.###.#####.####.#####.###.#",
        "#.###.#####.####.#####.###.#",
        "#.###.#####.####.#####.###.#",
        "#..........................#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#..........................#",
        "#.###.#####.####.#####.###.#",
        "#.###.#####.####.#####.###.#",
        "#.###.#####.####.#####.###.#",
        "#.###..................###.#",
        "#.###.#####.####.#####.###.#",
        "#.###.#####.####.#####.###.#",
        "#.###.#####.####.#####.###.#",
        "#....o......####......o....#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#.#########.####.#########.#",
        "#..... .............. .....#",
        "############################"
    ],
    "puntosReaparicion": [
        {"fila": 1, "columna": 6},
        {"fila": 1, "columna": 21},
        {"fila": 13, "columna": 1},
        {"fila": 17, "columna": 26},
        {"fila": 29, "columna": 6},
        {"fila": 29, "columna": 21}
    ],
    "posicionesIniciales": [
        {"idPersonaje": 1, "fila": 29, "columna": 6},
        {"idPersonaje": 2, "fila": 29, "columna": 21},
        {"idPersonaje": 3, "fila": 9, "columna": 11},
        {"idPersonaje": 4, "fila": 9, "columna": 13},
        {"idPersonaje": 5, "fila": 9, "columna": 16}
    ]
}', 1.00);

    -- Reconoce solo el JSON legado: filas 31, columnas 28 y casillas vacías,
    -- sin otros campos. El orden de propiedades y el espacio en blanco no importan.
    -- UPDATE conserva id_nivel, nombre y velocidad_base, también si hay partidas asociadas.
    UPDATE n
    SET laberinto_json = predeterminado.laberinto_json
    FROM niveles n
    INNER JOIN @NivelesPredeterminados predeterminado ON predeterminado.numero = n.numero
    WHERE n.numero = 1
        AND (SELECT COUNT(*) FROM OPENJSON(n.laberinto_json)) = 3
        AND EXISTS (
            SELECT 1 FROM OPENJSON(n.laberinto_json)
            WHERE [key] = 'filas' AND [type] = 2 AND [value] = '31'
        )
        AND EXISTS (
            SELECT 1 FROM OPENJSON(n.laberinto_json)
            WHERE [key] = 'columnas' AND [type] = 2 AND [value] = '28'
        )
        AND EXISTS (
            SELECT 1 FROM OPENJSON(n.laberinto_json)
            WHERE [key] = 'casillas' AND [type] = 4
        )
        AND NOT EXISTS (SELECT 1 FROM OPENJSON(n.laberinto_json, '$.casillas'));

    INSERT INTO niveles (numero, nombre, laberinto_json, velocidad_base)
    SELECT predeterminado.numero, predeterminado.nombre,
        predeterminado.laberinto_json, predeterminado.velocidad_base
    FROM @NivelesPredeterminados predeterminado
    WHERE NOT EXISTS (
        SELECT 1 FROM niveles n WITH (UPDLOCK, HOLDLOCK)
        WHERE n.numero = predeterminado.numero
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
