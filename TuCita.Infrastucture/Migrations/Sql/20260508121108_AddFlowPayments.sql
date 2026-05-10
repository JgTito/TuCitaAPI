BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    CREATE TABLE [EstadoPago] (
        [IdEstadoPago] int NOT NULL IDENTITY,
        [Nombre] nvarchar(80) NOT NULL,
        [Descripcion] nvarchar(300) NULL,
        [EsEstadoFinal] bit NOT NULL DEFAULT CAST(0 AS bit),
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_EstadoPago] PRIMARY KEY ([IdEstadoPago])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    CREATE TABLE [Pago] (
        [IdPago] int NOT NULL IDENTITY,
        [IdNegocio] int NOT NULL,
        [IdCita] int NOT NULL,
        [IdEstadoPago] int NOT NULL,
        [Proveedor] nvarchar(40) NOT NULL,
        [CommerceOrder] nvarchar(100) NOT NULL,
        [FlowOrder] bigint NULL,
        [Token] nvarchar(150) NULL,
        [CheckoutUrl] nvarchar(1000) NULL,
        [Monto] decimal(18,2) NOT NULL,
        [Moneda] nvarchar(10) NOT NULL DEFAULT N'CLP',
        [Subject] nvarchar(250) NULL,
        [PayerEmail] nvarchar(150) NULL,
        [PaymentMethod] int NULL,
        [FlowStatus] int NULL,
        [FlowStatusNombre] nvarchar(80) NULL,
        [PaymentDataJson] nvarchar(max) NULL,
        [RawCreateResponseJson] nvarchar(max) NULL,
        [RawStatusResponseJson] nvarchar(max) NULL,
        [Error] nvarchar(1000) NULL,
        [FechaCreacion] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
        [FechaActualizacion] datetime2 NULL,
        [FechaPago] datetime2 NULL,
        [FechaExpiracion] datetime2 NULL,
        [FechaUltimaConsulta] datetime2 NULL,
        CONSTRAINT [PK_Pago] PRIMARY KEY ([IdPago]),
        CONSTRAINT [CK_Pago_Monto] CHECK ([Monto] >= 0),
        CONSTRAINT [FK_Pago_Cita] FOREIGN KEY ([IdNegocio], [IdCita]) REFERENCES [Cita] ([IdNegocio], [IdCita]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pago_EstadoPago] FOREIGN KEY ([IdEstadoPago]) REFERENCES [EstadoPago] ([IdEstadoPago]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pago_Negocio] FOREIGN KEY ([IdNegocio]) REFERENCES [Negocio] ([IdNegocio]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdEstadoPago', N'Nombre', N'Descripcion', N'EsEstadoFinal', N'Activo') AND [object_id] = OBJECT_ID(N'[EstadoPago]'))
        SET IDENTITY_INSERT [EstadoPago] ON;
    EXEC(N'INSERT INTO [EstadoPago] ([IdEstadoPago], [Nombre], [Descripcion], [EsEstadoFinal], [Activo])
    VALUES (1, N''Pendiente'', N''El pago fue creado y esta pendiente de confirmacion'', CAST(0 AS bit), CAST(1 AS bit)),
    (2, N''Pagado'', N''El pago fue confirmado correctamente'', CAST(1 AS bit), CAST(1 AS bit)),
    (3, N''Rechazado'', N''El pago fue rechazado por la pasarela'', CAST(1 AS bit), CAST(1 AS bit)),
    (4, N''Anulado'', N''El pago fue anulado o expiro antes de completarse'', CAST(1 AS bit), CAST(1 AS bit)),
    (5, N''Error'', N''Ocurrio un error al procesar el pago'', CAST(1 AS bit), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'IdEstadoPago', N'Nombre', N'Descripcion', N'EsEstadoFinal', N'Activo') AND [object_id] = OBJECT_ID(N'[EstadoPago]'))
        SET IDENTITY_INSERT [EstadoPago] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_EstadoPago_Nombre] ON [EstadoPago] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Pago_FlowOrder] ON [Pago] ([FlowOrder]) WHERE [FlowOrder] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    CREATE INDEX [IX_Pago_IdCita] ON [Pago] ([IdCita]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    CREATE INDEX [IX_Pago_IdEstadoPago] ON [Pago] ([IdEstadoPago]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    CREATE INDEX [IX_Pago_IdNegocio] ON [Pago] ([IdNegocio]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    CREATE INDEX [IX_Pago_IdNegocio_IdCita] ON [Pago] ([IdNegocio], [IdCita]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Pago_CommerceOrder] ON [Pago] ([CommerceOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Pago_Token] ON [Pago] ([Token]) WHERE [Token] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508121108_AddFlowPayments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508121108_AddFlowPayments', N'10.0.7');
END;

COMMIT;
GO

