-- =============================================
-- 01. BASE DE DATOS
-- Ejecutar los scripts en orden (01 a 07) en SQL Server Management Studio.
-- Se pueden volver a ejecutar: no duplican ni borran nada.
-- =============================================

IF DB_ID('PacmanDB') IS NULL
BEGIN
    -- Intercalación sin distinción de mayúsculas: "Juan" y "juan" son el mismo usuario
    CREATE DATABASE PacmanDB COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO
