-- =============================================
-- 00. BORRAR LA BASE DE DATOS (solo desarrollo)
-- Los scripts 01 a 07 crean lo que falta, pero no modifican tablas que
-- ya existen. Cuando cambia la estructura, ejecutar este script y luego
-- volver a ejecutar del 01 al 07.
-- ¡Borra todos los datos, incluidos los usuarios registrados!
-- =============================================

USE master;
GO

IF DB_ID('PacmanDB') IS NOT NULL
BEGIN
    -- Cierra las conexiones abiertas (por ejemplo, la API corriendo)
    ALTER DATABASE PacmanDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE PacmanDB;
END
GO
