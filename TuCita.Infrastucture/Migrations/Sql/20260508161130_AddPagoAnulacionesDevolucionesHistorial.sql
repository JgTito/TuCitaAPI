BEGIN TRANSACTION;
MERGE [dbo].[EstadoPago] AS target
USING (VALUES
    (N'Parcialmente devuelto', N'El pago fue confirmado y tiene una devolucion parcial registrada', CAST(0 AS bit), CAST(1 AS bit)),
    (N'Devuelto', N'El pago fue devuelto completamente', CAST(1 AS bit), CAST(1 AS bit))
) AS source ([Nombre], [Descripcion], [EsEstadoFinal], [Activo])
ON target.[Nombre] = source.[Nombre]
WHEN MATCHED THEN
    UPDATE SET
        [Descripcion] = source.[Descripcion],
        [EsEstadoFinal] = source.[EsEstadoFinal],
        [Activo] = source.[Activo]
WHEN NOT MATCHED THEN
    INSERT ([Nombre], [Descripcion], [EsEstadoFinal], [Activo])
    VALUES (source.[Nombre], source.[Descripcion], source.[EsEstadoFinal], source.[Activo]);

ALTER TABLE [Pago] ADD [AnuladoPorUserId] nvarchar(128) NULL;

ALTER TABLE [Pago] ADD [FechaAnulacion] datetime2 NULL;

ALTER TABLE [Pago] ADD [FechaUltimaDevolucion] datetime2 NULL;

ALTER TABLE [Pago] ADD [MontoDevuelto] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Pago] ADD [MotivoAnulacion] nvarchar(500) NULL;

ALTER TABLE [Pago] ADD [ReferenciaAnulacion] nvarchar(100) NULL;

CREATE TABLE [PagoHistorial] (
    [IdPagoHistorial] int NOT NULL IDENTITY,
    [IdPago] int NOT NULL,
    [IdNegocio] int NOT NULL,
    [IdCita] int NOT NULL,
    [TipoEvento] nvarchar(40) NOT NULL,
    [EstadoAnterior] nvarchar(80) NULL,
    [EstadoNuevo] nvarchar(80) NULL,
    [Monto] decimal(18,2) NULL,
    [Motivo] nvarchar(500) NULL,
    [Referencia] nvarchar(100) NULL,
    [UserId] nvarchar(128) NULL,
    [DatosJson] nvarchar(max) NULL,
    [FechaCreacion] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_PagoHistorial] PRIMARY KEY ([IdPagoHistorial]),
    CONSTRAINT [CK_PagoHistorial_Monto] CHECK ([Monto] IS NULL OR [Monto] >= 0),
    CONSTRAINT [FK_PagoHistorial_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PagoHistorial_Cita] FOREIGN KEY ([IdNegocio], [IdCita]) REFERENCES [Cita] ([IdNegocio], [IdCita]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PagoHistorial_Negocio] FOREIGN KEY ([IdNegocio]) REFERENCES [Negocio] ([IdNegocio]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PagoHistorial_Pago] FOREIGN KEY ([IdPago]) REFERENCES [Pago] ([IdPago]) ON DELETE NO ACTION
);

INSERT INTO [dbo].[PagoHistorial]
    ([IdPago], [IdNegocio], [IdCita], [TipoEvento], [EstadoAnterior], [EstadoNuevo], [Monto], [Motivo], [Referencia], [UserId], [DatosJson], [FechaCreacion])
SELECT
    pago.[IdPago],
    pago.[IdNegocio],
    pago.[IdCita],
    N'MigracionInicial',
    NULL,
    estado.[Nombre],
    pago.[Monto],
    N'Historial inicial generado al habilitar auditoria de pagos.',
    pago.[CommerceOrder],
    pago.[RegistradoPorUserId],
    NULL,
    COALESCE(pago.[FechaActualizacion], pago.[FechaCreacion])
FROM [dbo].[Pago] AS pago
INNER JOIN [dbo].[EstadoPago] AS estado
    ON estado.[IdEstadoPago] = pago.[IdEstadoPago]
WHERE NOT EXISTS (
    SELECT 1
    FROM [dbo].[PagoHistorial] AS historial
    WHERE historial.[IdPago] = pago.[IdPago]
);

CREATE INDEX [IX_Pago_AnuladoPorUserId] ON [Pago] ([AnuladoPorUserId]);

CREATE INDEX [IX_PagoHistorial_FechaCreacion] ON [PagoHistorial] ([FechaCreacion]);

CREATE INDEX [IX_PagoHistorial_IdNegocio] ON [PagoHistorial] ([IdNegocio]);

CREATE INDEX [IX_PagoHistorial_IdNegocio_IdCita] ON [PagoHistorial] ([IdNegocio], [IdCita]);

CREATE INDEX [IX_PagoHistorial_IdPago] ON [PagoHistorial] ([IdPago]);

CREATE INDEX [IX_PagoHistorial_UserId] ON [PagoHistorial] ([UserId]);

ALTER TABLE [Pago] ADD CONSTRAINT [FK_Pago_AnuladoPor] FOREIGN KEY ([AnuladoPorUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260508161130_AddPagoAnulacionesDevolucionesHistorial', N'10.0.7');

COMMIT;
GO

