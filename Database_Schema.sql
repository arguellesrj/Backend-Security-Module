CREATE TABLE CodigosRecuperacion
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(100) NOT NULL,
    CodigoHash CHAR(64) NOT NULL,              -- SHA-256 en texto hexadecimal siempre ocupa 64 caracteres
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaExpiracion DATETIME2 NOT NULL,
    Usado BIT NOT NULL DEFAULT 0,              -- 0 = Activo, 1 = Consumido
    IntentosFallidos INT NOT NULL DEFAULT 0,   -- Control de fuerza bruta por código
    
    -- Índice para acelerar la búsqueda del código
    INDEX IX_CodigosRecuperacion_Email_Hash (Email, CodigoHash)
);

CREATE PROCEDURE sp_GuardarCodigoRecuperacion
    @Email NVARCHAR(100),
    @CodigoHash CHAR(64),
    @FechaExpiracion DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        -- 1. Invalidar códigos activos anteriores del mismo email
        UPDATE CodigosRecuperacion
        SET Usado = 1
        WHERE Email = @Email AND Usado = 0;

        -- 2. Insertar el nuevo código
        INSERT INTO CodigosRecuperacion (Email, CodigoHash, FechaExpiracion, Usado, IntentosFallidos)
        VALUES (@Email, @CodigoHash, @FechaExpiracion, 0, 0);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;

CREATE PROCEDURE sp_ValidarCodigoRecuperacion
    @Email NVARCHAR(100),
    @CodigoHash CHAR(64),
    @EsValido BIT OUTPUT,
    @Mensaje NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Id BIGINT;
    DECLARE @FechaExpiracion DATETIME2;
    DECLARE @Usado BIT;
    DECLARE @Intentos INT;

    -- Buscar el registro más reciente para este correo y hash
    SELECT TOP 1 
        @Id = Id,
        @FechaExpiracion = FechaExpiracion,
        @Usado = Usado,
        @Intentos = IntentosFallidos
    FROM CodigosRecuperacion
    WHERE Email = @Email AND CodigoHash = @CodigoHash
    ORDER BY FechaCreacion DESC;

    -- 1. Verificar si existe
    IF @Id IS NULL
    BEGIN
        SET @EsValido = 0;
        SET @Mensaje = 'Código incorrecto.';
        RETURN;
    END

    -- 2. Verificar si ya fue usado
    IF @Usado = 1
    BEGIN
        SET @EsValido = 0;
        SET @Mensaje = 'El código ya ha sido utilizado.';
        RETURN;
    END

    -- 3. Verificar límite de intentos fallidos
    IF @Intentos >= 3
    BEGIN
        SET @EsValido = 0;
        SET @Mensaje = 'Código bloqueado por demasiados intentos fallidos.';
        RETURN;
    END

    -- 4. Verificar expiración
    IF SYSDATETIME() > @FechaExpiracion
    BEGIN
        SET @EsValido = 0;
        SET @Mensaje = 'El código ha expirado.';
        RETURN;
    END

    -- Si pasa todas las validaciones:
    SET @EsValido = 1;
    SET @Mensaje = 'Código válido.';

    -- Marcar el código como consumido/usado
    UPDATE CodigosRecuperacion
    SET Usado = 1
    WHERE Id = @Id;
END;

-- Elimina códigos que tengan más de 30 días de creados
DELETE FROM CodigosRecuperacion
WHERE FechaCreacion < DATEADD(DAY, -30, SYSDATETIME());

ALTER PROCEDURE sp_ValidarCodigoRecuperacion
    @Email NVARCHAR(100),
    @CodigoHash CHAR(64),
    @EsValido BIT OUTPUT,
    @Mensaje NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Id BIGINT;
    DECLARE @FechaExpiracion DATETIME2;
    DECLARE @Usado BIT;
    DECLARE @Intentos INT;

    -- Buscar el último código activo/no usado de este correo
    SELECT TOP 1 
        @Id = Id,
        @FechaExpiracion = FechaExpiracion,
        @Usado = Usado,
        @Intentos = IntentosFallidos
    FROM CodigosRecuperacion
    WHERE Email = @Email AND Usado = 0
    ORDER BY FechaCreacion DESC;

    IF @Id IS NULL
    BEGIN
        SET @EsValido = 0;
        SET @Mensaje = 'No hay solicitudes de código activas para este correo.';
        RETURN;
    END

    -- Si el hash enviado NO coincide con el de la BD, incrementamos el intento fallido
    IF (SELECT CodigoHash FROM CodigosRecuperacion WHERE Id = @Id) <> @CodigoHash
    BEGIN
        UPDATE CodigosRecuperacion 
        SET IntentosFallidos = IntentosFallidos + 1 
        WHERE Id = @Id;

        SET @EsValido = 0;
        SET @Mensaje = 'Código incorrecto.';
        RETURN;
    END
END;