-- =============================================
-- 03. RELACIONES (llaves foráneas)
-- =============================================

USE PacmanDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_sesiones_usuarios')
ALTER TABLE sesiones_moviles
    ADD CONSTRAINT FK_sesiones_usuarios FOREIGN KEY (id_usuario)
    REFERENCES usuarios (id_usuario) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_partidas_dificultades')
ALTER TABLE partidas
    ADD CONSTRAINT FK_partidas_dificultades FOREIGN KEY (id_dificultad)
    REFERENCES dificultades (id_dificultad);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_rondas_partidas')
ALTER TABLE rondas
    ADD CONSTRAINT FK_rondas_partidas FOREIGN KEY (id_partida)
    REFERENCES partidas (id_partida) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_rondas_niveles')
ALTER TABLE rondas
    ADD CONSTRAINT FK_rondas_niveles FOREIGN KEY (id_nivel)
    REFERENCES niveles (id_nivel);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_participantes_rondas')
ALTER TABLE participantes_ronda
    ADD CONSTRAINT FK_participantes_rondas FOREIGN KEY (id_ronda)
    REFERENCES rondas (id_ronda) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_participantes_usuarios')
ALTER TABLE participantes_ronda
    ADD CONSTRAINT FK_participantes_usuarios FOREIGN KEY (id_usuario)
    REFERENCES usuarios (id_usuario) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_participantes_personajes')
ALTER TABLE participantes_ronda
    ADD CONSTRAINT FK_participantes_personajes FOREIGN KEY (id_personaje)
    REFERENCES personajes (id_personaje);
GO
