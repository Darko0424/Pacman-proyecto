-- =============================================
-- 04. ÍNDICES
-- =============================================

USE PacmanDB;
GO

-- Buscar la sesión por su token
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sesiones_token' AND object_id = OBJECT_ID('dbo.sesiones_moviles'))
CREATE NONCLUSTERED INDEX IX_sesiones_token
    ON sesiones_moviles (token_dispositivo);
GO

-- Partidas más recientes primero
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_partidas_fecha' AND object_id = OBJECT_ID('dbo.partidas'))
CREATE NONCLUSTERED INDEX IX_partidas_fecha
    ON partidas (fecha_inicio DESC);
GO

-- Un usuario solo puede participar una vez por partida (la máquina no cuenta)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_participantes_usuario' AND object_id = OBJECT_ID('dbo.participantes_partida'))
CREATE UNIQUE NONCLUSTERED INDEX UX_participantes_usuario
    ON participantes_partida (id_partida, id_usuario)
    WHERE id_usuario IS NOT NULL;
GO

-- Historial y ranking por usuario
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_participantes_usuario' AND object_id = OBJECT_ID('dbo.participantes_partida'))
CREATE NONCLUSTERED INDEX IX_participantes_usuario
    ON participantes_partida (id_usuario)
    INCLUDE (id_partida, id_personaje, puntuacion);
GO
