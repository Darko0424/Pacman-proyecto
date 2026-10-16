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

-- Una sala no inicia dos partidas en el mismo instante: evita duplicados al reintentar
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_partidas_sala_inicio' AND object_id = OBJECT_ID('dbo.partidas'))
CREATE UNIQUE NONCLUSTERED INDEX UX_partidas_sala_inicio
    ON partidas (codigo_sala, fecha_inicio);
GO

-- Un usuario solo controla un personaje por ronda (la máquina no cuenta)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_participantes_usuario' AND object_id = OBJECT_ID('dbo.participantes_ronda'))
CREATE UNIQUE NONCLUSTERED INDEX UX_participantes_usuario
    ON participantes_ronda (id_ronda, id_usuario)
    WHERE id_usuario IS NOT NULL;
GO

-- Historial y ranking por usuario
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_participantes_usuario' AND object_id = OBJECT_ID('dbo.participantes_ronda'))
CREATE NONCLUSTERED INDEX IX_participantes_usuario
    ON participantes_ronda (id_usuario)
    INCLUDE (id_ronda, id_personaje, puntuacion);
GO
