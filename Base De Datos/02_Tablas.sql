-- =============================================
-- 02. TABLAS
-- =============================================

USE PacmanDB;
GO

-- ---------- Usuarios y sesiones ----------

IF OBJECT_ID('dbo.usuarios', 'U') IS NULL
CREATE TABLE usuarios (
    id_usuario          INT IDENTITY(1,1) PRIMARY KEY,
    nombre_usuario      VARCHAR(50)  NOT NULL UNIQUE,
    correo_electronico  VARCHAR(100) NOT NULL UNIQUE,
    contrasena_hash     VARCHAR(255) NOT NULL,
    fecha_registro      DATETIME2    NOT NULL DEFAULT GETDATE()
);
GO

-- token_dispositivo guarda el hash SHA-256 del token de sesión, no el token
IF OBJECT_ID('dbo.sesiones_moviles', 'U') IS NULL
CREATE TABLE sesiones_moviles (
    id_sesion           INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario          INT          NOT NULL,
    token_dispositivo   VARCHAR(255) NOT NULL,
    ultimo_acceso       DATETIME2    NOT NULL DEFAULT GETDATE()
);
GO

-- ---------- Catálogos ----------

IF OBJECT_ID('dbo.personajes', 'U') IS NULL
CREATE TABLE personajes (
    id_personaje        TINYINT      PRIMARY KEY,
    nombre              VARCHAR(20)  NOT NULL UNIQUE,
    rol                 VARCHAR(10)  NOT NULL CHECK (rol IN ('PACMAN', 'FANTASMA')),
    color               VARCHAR(15)  NOT NULL UNIQUE
);
GO

IF OBJECT_ID('dbo.dificultades', 'U') IS NULL
CREATE TABLE dificultades (
    id_dificultad               TINYINT      PRIMARY KEY,
    nombre                      VARCHAR(15)  NOT NULL UNIQUE,
    multiplicador_velocidad_ia  DECIMAL(3,2) NOT NULL CHECK (multiplicador_velocidad_ia > 0),
    duracion_asustado_seg       TINYINT      NOT NULL CHECK (duracion_asustado_seg > 0)
);
GO

IF OBJECT_ID('dbo.niveles', 'U') IS NULL
CREATE TABLE niveles (
    id_nivel            INT IDENTITY(1,1) PRIMARY KEY,
    numero              INT           NOT NULL UNIQUE CHECK (numero > 0),
    nombre              VARCHAR(50)   NOT NULL,
    laberinto_json      NVARCHAR(MAX) NOT NULL CHECK (ISJSON(laberinto_json) = 1),
    velocidad_base      DECIMAL(4,2)  NOT NULL CHECK (velocidad_base > 0)
);
GO

IF OBJECT_ID('dbo.frutas', 'U') IS NULL
CREATE TABLE frutas (
    id_fruta            TINYINT      PRIMARY KEY,
    nombre              VARCHAR(30)  NOT NULL UNIQUE,
    efecto              VARCHAR(30)  NOT NULL CHECK (efecto IN ('VELOCIDAD', 'CONGELAR_FANTASMAS', 'INVISIBILIDAD', 'DOBLE_PUNTAJE')),
    duracion_seg        TINYINT      NOT NULL CHECK (duracion_seg > 0),
    puntos              INT          NOT NULL CHECK (puntos >= 0)
);
GO

-- ---------- Partidas ----------

IF OBJECT_ID('dbo.partidas', 'U') IS NULL
CREATE TABLE partidas (
    id_partida          INT IDENTITY(1,1) PRIMARY KEY,
    codigo_sala         VARCHAR(6)   NULL,
    modo                VARCHAR(15)  NOT NULL CHECK (modo IN ('MULTIJUGADOR', 'CONTRA_IA')),
    id_dificultad       TINYINT      NOT NULL,
    id_nivel_alcanzado  INT          NOT NULL,
    estado              VARCHAR(12)  NOT NULL DEFAULT 'EN_CURSO'
                        CHECK (estado IN ('EN_CURSO', 'FINALIZADA', 'ABANDONADA')),
    bando_ganador       VARCHAR(10)  NULL CHECK (bando_ganador IN ('PACMAN', 'FANTASMAS')),
    fecha_inicio        DATETIME2    NOT NULL DEFAULT GETDATE(),
    fecha_fin           DATETIME2    NULL,

    CONSTRAINT CK_partidas_fechas
        CHECK (fecha_fin IS NULL OR fecha_fin >= fecha_inicio),
    -- Solo las partidas multijugador tienen código de sala
    CONSTRAINT CK_partidas_sala_modo
        CHECK ((modo = 'MULTIJUGADOR' AND codigo_sala IS NOT NULL)
            OR (modo = 'CONTRA_IA'    AND codigo_sala IS NULL))
);
GO

IF OBJECT_ID('dbo.participantes_partida', 'U') IS NULL
CREATE TABLE participantes_partida (
    id_participante     INT IDENTITY(1,1) PRIMARY KEY,
    id_partida          INT      NOT NULL,
    id_usuario          INT      NULL,
    id_personaje        TINYINT  NOT NULL,
    es_ia               BIT      NOT NULL DEFAULT 0,
    puntuacion          INT      NOT NULL DEFAULT 0,
    fantasmas_comidos   INT      NOT NULL DEFAULT 0,
    pacmans_atrapados   INT      NOT NULL DEFAULT 0,
    frutas_comidas      INT      NOT NULL DEFAULT 0,
    abandono            BIT      NOT NULL DEFAULT 0,

    -- Un personaje solo puede aparecer una vez por partida
    CONSTRAINT UQ_participantes_personaje UNIQUE (id_partida, id_personaje),
    -- La máquina no tiene usuario; un jugador humano siempre lo tiene
    CONSTRAINT CK_participantes_ia
        CHECK ((es_ia = 1 AND id_usuario IS NULL)
            OR (es_ia = 0 AND id_usuario IS NOT NULL))
);
GO
