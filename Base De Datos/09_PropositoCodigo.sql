-- =============================================
-- 09. PROPOSITO DEL CODIGO POR CORREO
-- El mismo codigo de 6 dogitos sirve para verificar la cuenta, recuperar la
-- contrasena o cambiarla. Esta columna indica para cual se envia y asi un
-- codigo no se puede usar para otra cosa.
-- =============================================

USE PacmanDB;
GO

IF COL_LENGTH('dbo.usuarios', 'proposito_codigo') IS NULL
BEGIN
    ALTER TABLE usuarios ADD
        proposito_codigo VARCHAR(10) NULL
            CONSTRAINT CK_usuarios_proposito_codigo
            CHECK (proposito_codigo IN ('VERIFICAR', 'RECUPERAR', 'CAMBIAR'));

    -- Los c?digos pendientes que ya exist?an eran de verificaci?n de cuenta
    EXEC('UPDATE usuarios SET proposito_codigo = ''VERIFICAR''
          WHERE codigo_verificacion_hash IS NOT NULL;');
END
GO