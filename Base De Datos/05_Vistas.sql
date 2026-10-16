-- =============================================
-- 05. VISTAS
-- El progreso y el ranking se calculan desde las rondas,
-- así nunca quedan desactualizados.
-- Las partidas contra la IA cuentan para el historial y el progreso,
-- pero no para los rankings.
-- =============================================

USE PacmanDB;
GO

-- Historial: una fila por usuario y partida (suma de todos sus niveles)
CREATE OR ALTER VIEW vw_historial_usuario AS
SELECT
    pr.id_usuario,
    p.id_partida,
    p.codigo_sala,
    p.modo,
    d.nombre                    AS dificultad,
    p.estado,
    p.fecha_inicio,
    p.fecha_fin,
    COUNT(*)                    AS niveles_jugados,
    MAX(n.numero)               AS nivel_alcanzado,
    SUM(pr.puntuacion)          AS puntuacion,
    SUM(pr.fantasmas_comidos)   AS fantasmas_comidos,
    SUM(pr.pacmans_atrapados)   AS pacmans_atrapados,
    SUM(pr.objetos_usados)      AS objetos_usados,
    SUM(CASE WHEN r.bando_ganador = pe.rol THEN 1 ELSE 0 END) AS niveles_ganados,
    CAST(MAX(CAST(pr.abandono AS TINYINT)) AS BIT)            AS abandono
FROM participantes_ronda pr
INNER JOIN rondas       r  ON r.id_ronda       = pr.id_ronda
INNER JOIN partidas     p  ON p.id_partida     = r.id_partida
INNER JOIN dificultades d  ON d.id_dificultad  = p.id_dificultad
INNER JOIN niveles      n  ON n.id_nivel       = r.id_nivel
INNER JOIN personajes   pe ON pe.id_personaje  = pr.id_personaje
WHERE pr.id_usuario IS NOT NULL
GROUP BY pr.id_usuario, p.id_partida, p.codigo_sala, p.modo, d.nombre,
    p.estado, p.fecha_inicio, p.fecha_fin;
GO

-- Detalle del historial: una fila por usuario y nivel jugado
CREATE OR ALTER VIEW vw_historial_rondas AS
SELECT
    pr.id_usuario,
    r.id_partida,
    r.id_ronda,
    n.numero        AS numero_nivel,
    n.nombre        AS nivel,
    pe.nombre       AS personaje,
    pe.rol,
    pr.puntuacion,
    pr.fantasmas_comidos,
    pr.pacmans_atrapados,
    pr.objetos_usados,
    pr.abandono,
    r.bando_ganador,
    -- 1 = ganó, 0 = perdió, NULL = ronda interrumpida
    CASE
        WHEN r.bando_ganador IS NULL THEN NULL
        WHEN r.bando_ganador = pe.rol THEN 1
        ELSE 0
    END AS gano,
    r.fecha_inicio,
    r.fecha_fin
FROM participantes_ronda pr
INNER JOIN rondas     r  ON r.id_ronda      = pr.id_ronda
INNER JOIN niveles    n  ON n.id_nivel      = r.id_nivel
INNER JOIN personajes pe ON pe.id_personaje = pr.id_personaje
WHERE pr.id_usuario IS NOT NULL;
GO

-- Progreso general de cada usuario (solo partidas finalizadas, incluye contra la IA)
CREATE OR ALTER VIEW vw_progreso_jugador AS
SELECT
    u.id_usuario,
    u.nombre_usuario,
    COUNT(t.id_partida)             AS partidas_jugadas,
    ISNULL(SUM(t.puntuacion), 0)    AS puntos_totales,
    ISNULL(MAX(t.puntuacion), 0)    AS mejor_puntuacion,
    ISNULL(MAX(t.nivel_alcanzado), 1) AS nivel_maximo_alcanzado
FROM usuarios u
LEFT JOIN (
    SELECT pr.id_usuario, r.id_partida,
        SUM(pr.puntuacion) AS puntuacion,
        MAX(n.numero)      AS nivel_alcanzado
    FROM participantes_ronda pr
    INNER JOIN rondas   r ON r.id_ronda   = pr.id_ronda
    INNER JOIN partidas p ON p.id_partida = r.id_partida AND p.estado = 'FINALIZADA'
    INNER JOIN niveles  n ON n.id_nivel   = r.id_nivel
    WHERE pr.id_usuario IS NOT NULL
    GROUP BY pr.id_usuario, r.id_partida
) t ON t.id_usuario = u.id_usuario
GROUP BY u.id_usuario, u.nombre_usuario;
GO

-- Ranking por rol: mejor puntuación en un nivel jugando ese rol (solo multijugador)
CREATE OR ALTER VIEW vw_ranking AS
SELECT
    pe.rol,
    u.id_usuario,
    u.nombre_usuario,
    MAX(pr.puntuacion)  AS mejor_puntuacion,
    COUNT(*)            AS niveles_en_rol,
    RANK() OVER (PARTITION BY pe.rol ORDER BY MAX(pr.puntuacion) DESC) AS posicion
FROM participantes_ronda pr
INNER JOIN rondas     r  ON r.id_ronda      = pr.id_ronda
INNER JOIN partidas   p  ON p.id_partida    = r.id_partida
                         AND p.estado = 'FINALIZADA' AND p.modo = 'MULTIJUGADOR'
INNER JOIN personajes pe ON pe.id_personaje = pr.id_personaje
INNER JOIN usuarios   u  ON u.id_usuario    = pr.id_usuario
GROUP BY pe.rol, u.id_usuario, u.nombre_usuario;
GO

-- Ranking total: suma de puntos de ambos roles (solo multijugador)
CREATE OR ALTER VIEW vw_ranking_total AS
SELECT
    u.id_usuario,
    u.nombre_usuario,
    SUM(pr.puntuacion)              AS puntos_totales,
    COUNT(DISTINCT r.id_partida)    AS partidas_jugadas,
    RANK() OVER (ORDER BY SUM(pr.puntuacion) DESC) AS posicion
FROM participantes_ronda pr
INNER JOIN rondas   r ON r.id_ronda   = pr.id_ronda
INNER JOIN partidas p ON p.id_partida = r.id_partida
                      AND p.estado = 'FINALIZADA' AND p.modo = 'MULTIJUGADOR'
INNER JOIN usuarios u ON u.id_usuario = pr.id_usuario
GROUP BY u.id_usuario, u.nombre_usuario;
GO
