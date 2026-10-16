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

-- Objetos de las cajas: cada uno pertenece a un bando (mismos valores que personajes.rol).
-- efecto no tiene CHECK: los objetos aún se están definiendo y el motor valida la clave.
IF OBJECT_ID('dbo.objetos', 'U') IS NULL
CREATE TABLE objetos (
    id_objeto           TINYINT      PRIMARY KEY,
    nombre              VARCHAR(30)  NOT NULL UNIQUE,
    bando               VARCHAR(10)  NOT NULL CHECK (bando IN ('PACMAN', 'FANTASMA')),
    efecto              VARCHAR(30)  NOT NULL,
    duracion_seg        TINYINT      NOT NULL CHECK (duracion_seg > 0)
);
GO

-- ---------- Partidas ----------
-- Una partida es la serie completa de niveles que se juega en una sala.
-- Cada nivel jugado es una ronda, y los participantes se guardan por ronda
-- porque los roles se vuelven a sortear en cada nivel.

IF OBJECT_ID('dbo.partidas', 'U') IS NULL
CREATE TABLE partidas (
    id_partida          INT IDENTITY(1,1) PRIMARY KEY,
    -- También las partidas contra la IA se inician desde una sala
    codigo_sala         VARCHAR(6)   NOT NULL,
    modo                VARCHAR(15)  NOT NULL CHECK (modo IN ('MULTIJUGADOR', 'CONTRA_IA')),
    id_dificultad       TINYINT      NOT NULL,
    estado              VARCHAR(12)  NOT NULL DEFAULT 'EN_CURSO'
                        CHECK (estado IN ('EN_CURSO', 'FINALIZADA', 'ABANDONADA')),
    fecha_inicio        DATETIME2    NOT NULL,
    fecha_fin           DATETIME2    NULL,

    CONSTRAINT CK_partidas_fechas
        CHECK (fecha_fin IS NULL OR fecha_fin >= fecha_inicio),
    -- Solo las partidas terminadas tienen fecha de fin
    CONSTRAINT CK_partidas_fin
        CHECK ((estado = 'EN_CURSO' AND fecha_fin IS NULL)
            OR (estado <> 'EN_CURSO' AND fecha_fin IS NOT NULL))
);
GO

IF OBJECT_ID('dbo.rondas', 'U') IS NULL
CREATE TABLE rondas (
    id_ronda            INT IDENTITY(1,1) PRIMARY KEY,
    id_partida          INT          NOT NULL,
    id_nivel            INT          NOT NULL,
    -- NULL = ronda interrumpida, sin ganador
    bando_ganador       VARCHAR(10)  NULL CHECK (bando_ganador IN ('PACMAN', 'FANTASMA')),
    fecha_inicio        DATETIME2    NOT NULL,
    fecha_fin           DATETIME2    NOT NULL,

    -- Cada nivel se juega una sola vez por partida
    CONSTRAINT UQ_rondas_partida_nivel UNIQUE (id_partida, id_nivel),
    CONSTRAINT CK_rondas_fechas CHECK (fecha_fin >= fecha_inicio)
);
GO

IF OBJECT_ID('dbo.participantes_ronda', 'U') IS NULL
CREATE TABLE participantes_ronda (
    id_participante     INT IDENTITY(1,1) PRIMARY KEY,
    id_ronda            INT      NOT NULL,
    id_usuario          INT      NULL,
    id_personaje        TINYINT  NOT NULL,
    es_ia               BIT      NOT NULL DEFAULT 0,
    puntuacion          INT      NOT NULL DEFAULT 0 CHECK (puntuacion >= 0),
    fantasmas_comidos   INT      NOT NULL DEFAULT 0 CHECK (fantasmas_comidos >= 0),
    pacmans_atrapados   INT      NOT NULL DEFAULT 0 CHECK (pacmans_atrapados >= 0),
    objetos_usados      INT      NOT NULL DEFAULT 0 CHECK (objetos_usados >= 0),
    -- El jugador se fue y una IA terminó la ronda con su personaje
    abandono            BIT      NOT NULL DEFAULT 0,

    -- Un personaje solo puede aparecer una vez por ronda
    CONSTRAINT UQ_participantes_personaje UNIQUE (id_ronda, id_personaje),
    -- La máquina no tiene usuario; un jugador humano siempre lo tiene
    CONSTRAINT CK_participantes_ia
        CHECK ((es_ia = 1 AND id_usuario IS NULL)
            OR (es_ia = 0 AND id_usuario IS NOT NULL)),
    -- Solo un humano puede abandonar
    CONSTRAINT CK_participantes_abandono CHECK (abandono = 0 OR es_ia = 0)
);
GO
