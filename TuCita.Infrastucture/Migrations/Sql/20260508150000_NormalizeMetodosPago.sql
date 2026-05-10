IF OBJECT_ID(N'[dbo].[MetodoPago]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MetodoPago] (
        [IdMetodoPago] int IDENTITY(1,1) NOT NULL,
        [Nombre] nvarchar(80) NOT NULL,
        [Descripcion] nvarchar(300) NULL,
        [EsManual] bit NOT NULL CONSTRAINT [DF_MetodoPago_EsManual] DEFAULT CAST(0 AS bit),
        [EsOnline] bit NOT NULL CONSTRAINT [DF_MetodoPago_EsOnline] DEFAULT CAST(0 AS bit),
        [Activo] bit NOT NULL CONSTRAINT [DF_MetodoPago_Activo] DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_MetodoPago] PRIMARY KEY ([IdMetodoPago])
    );

    CREATE UNIQUE INDEX [UQ_MetodoPago_Nombre] ON [dbo].[MetodoPago] ([Nombre]);
END;
GO

SET IDENTITY_INSERT [dbo].[MetodoPago] ON;

MERGE [dbo].[MetodoPago] AS target
USING (VALUES
    (1, N'Flow', N'Pago online procesado por Flow', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit)),
    (2, N'Efectivo', N'Pago manual recibido en efectivo en el negocio', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit)),
    (3, N'Transferencia bancaria', N'Pago manual recibido por transferencia bancaria', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit)),
    (4, N'Tarjeta debito', N'Pago manual recibido con tarjeta de debito', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit)),
    (5, N'Tarjeta credito', N'Pago manual recibido con tarjeta de credito', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit)),
    (6, N'POS', N'Pago manual recibido mediante terminal POS o red de pago', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit)),
    (7, N'Cheque', N'Pago manual recibido con cheque', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit)),
    (8, N'Otro', N'Otro metodo de pago manual', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit))
) AS source ([IdMetodoPago], [Nombre], [Descripcion], [EsManual], [EsOnline], [Activo])
ON target.[IdMetodoPago] = source.[IdMetodoPago]
WHEN MATCHED THEN
    UPDATE SET
        [Nombre] = source.[Nombre],
        [Descripcion] = source.[Descripcion],
        [EsManual] = source.[EsManual],
        [EsOnline] = source.[EsOnline],
        [Activo] = source.[Activo]
WHEN NOT MATCHED THEN
    INSERT ([IdMetodoPago], [Nombre], [Descripcion], [EsManual], [EsOnline], [Activo])
    VALUES (source.[IdMetodoPago], source.[Nombre], source.[Descripcion], source.[EsManual], source.[EsOnline], source.[Activo]);

SET IDENTITY_INSERT [dbo].[MetodoPago] OFF;
GO

IF COL_LENGTH(N'[dbo].[Pago]', N'IdMetodoPago') IS NULL
BEGIN
    ALTER TABLE [dbo].[Pago] ADD [IdMetodoPago] int NULL;

    UPDATE [dbo].[Pago]
    SET [IdMetodoPago] =
        CASE
            WHEN [MetodoPago] = N'Efectivo' THEN 2
            WHEN [MetodoPago] = N'Transferencia bancaria' THEN 3
            WHEN [MetodoPago] = N'Tarjeta debito' THEN 4
            WHEN [MetodoPago] = N'Tarjeta credito' THEN 5
            WHEN [MetodoPago] = N'POS' THEN 6
            WHEN [MetodoPago] = N'Cheque' THEN 7
            WHEN [MetodoPago] = N'Otro' THEN 8
            WHEN [Proveedor] = N'Manual' OR [EsManual] = 1 THEN 2
            ELSE 1
        END;

    ALTER TABLE [dbo].[Pago] ALTER COLUMN [IdMetodoPago] int NOT NULL;
END;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pago_MetodoPago' AND object_id = OBJECT_ID(N'[dbo].[Pago]'))
BEGIN
    DROP INDEX [IX_Pago_MetodoPago] ON [dbo].[Pago];
END;
GO

IF COL_LENGTH(N'[dbo].[Pago]', N'MetodoPago') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Pago] DROP COLUMN [MetodoPago];
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pago_IdMetodoPago' AND object_id = OBJECT_ID(N'[dbo].[Pago]'))
BEGIN
    CREATE INDEX [IX_Pago_IdMetodoPago] ON [dbo].[Pago] ([IdMetodoPago]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Pago_MetodoPago')
BEGIN
    ALTER TABLE [dbo].[Pago] WITH CHECK ADD CONSTRAINT [FK_Pago_MetodoPago]
        FOREIGN KEY ([IdMetodoPago]) REFERENCES [dbo].[MetodoPago] ([IdMetodoPago]);
END;
GO
