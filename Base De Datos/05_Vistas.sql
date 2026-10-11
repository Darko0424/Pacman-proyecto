-- =============================================
-- 05. VISTAS
-- El progreso y el ranking se calculan desde las partidas,
-- así nunca quedan desactualizados.
-- =============================================

USE PacmanDB;
GO

-- Progreso general de cada usuario (solo partidas finalizadas)
CREATE OR ALTER VIEW vw_progreso_jugador AS
SELECT
    u.id_usuario,
    u.nombre_usuario,
    COUNT(pp.id_participante)       AS partidas_jugadas,
    ISNULL(SUM(pp.puntuacion), 0)   AS puntos_totales,
    ISNULL(MAX(pp.puntuacion), 0)   AS mejor_puntuacion,
    ISNULL(MAX(n.numero), 1)        AS nivel_maximo_alcanzado
FROM usuarios u
LEFT JOIN (
    participantes_partida pp
    INNER JOIN partidas p ON p.id_partida = pp.id_partida AND p.estado = 'FINALIZADA'
    INNER JOIN niveles  n ON n.id_nivel   = p.id_nivel_alcanzado
) ON pp.id_usuario = u.id_usuario
GROUP BY u.id_usuario, u.nombre_usuario;
GO

-- Historial detallado de partidas por usuario
CREATE OR ALTER VIEW vw_historial_usuario AS
SELECT
    pp.id_usuario,
    p.id_partida,
    p.fecha_inicio,
    p.fecha_fin,
    p.modo,
    d.nombre        AS dificultad,
    n.numero        AS nivel_alcanzado,
    pe.nombre       AS personaje,
    pe.rol,
    pp.puntuacion,
    pp.fantasmas_comidos,
    pp.pacmans_atrapados,
    pp.frutas_comidas,
    p.estado,
    -- 1 = ganó, 0 = perdió, NULL = sin resultado (abandonada o en curso)
    CASE
        WHEN p.bando_ganador IS NULL THEN NULL
        WHEN (pe.rol = 'PACMAN'   AND p.bando_ganador = 'PACMAN')
          OR (pe.rol = 'FANTASMA' AND p.bando_ganador = 'FANTASMAS') THEN 1
        ELSE 0
    END AS gano
FROM participantes_partida pp
INNER JOIN partidas     p  ON p.id_partida     = pp.id_partida
INNER JOIN dificultades d  ON d.id_dificultad  = p.id_dificultad
INNER JOIN niveles      n  ON n.id_nivel       = p.id_nivel_alcanzado
INNER JOIN personajes   pe ON pe.id_personaje  = pp.id_personaje
WHERE pp.id_usuario IS NOT NULL;
GO

-- Ranking separado por rol (mejor Pac-Man y mejor fantasma)
CREATE OR ALTER VIEW vw_ranking AS
SELECT
    pe.rol,
    u.id_usuario,
    u.nombre_usuario,
    MAX(pp.puntuacion)  AS mejor_puntuacion,
    COUNT(*)            AS partidas_en_rol,
    RANK() OVER (PARTITION BY pe.rol ORDER BY MAX(pp.puntuacion) DESC) AS posicion
FROM participantes_partida pp
INNER JOIN partidas   p  ON p.id_partida    = pp.id_partida AND p.estado = 'FINALIZADA'
INNER JOIN personajes pe ON pe.id_personaje = pp.id_personaje
INNER JOIN usuarios   u  ON u.id_usuario    = pp.id_usuario
GROUP BY pe.rol, u.id_usuario, u.nombre_usuario;
GO
