-- =============================================
-- 08. VERIFICACIÓN DE CORREO Y NOMBRE DE USUARIO
-- =============================================

USE PacmanDB;
GO

IF COL_LENGTH('dbo.usuarios', 'correo_verificado') IS NULL
BEGIN
    ALTER TABLE usuarios ADD
        correo_verificado        BIT          NOT NULL CONSTRAINT DF_usuarios_correo_verificado DEFAULT 0,
        -- 0 = todavía tiene el nombre temporal que le dio el sistema
        nombre_definido          BIT          NOT NULL CONSTRAINT DF_usuarios_nombre_definido DEFAULT 0,
        -- Hash del código de 6 dígitos (nunca el código en texto plano)
        codigo_verificacion_hash VARCHAR(255) NULL,
        codigo_expira            DATETIME2    NULL,
        -- Para limitar los reenvíos (uno por minuto)
        codigo_enviado           DATETIME2    NULL,
        intentos_codigo          TINYINT      NOT NULL CONSTRAINT DF_usuarios_intentos_codigo DEFAULT 0;

    -- Las cuentas que ya existían quedan verificadas y con su nombre
    EXEC('UPDATE usuarios SET correo_verificado = 1, nombre_definido = 1;');
END
GO