BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508143000_AddManualPayments'
)
BEGIN
    ALTER TABLE [Pago] ADD [EsManual] bit NOT NULL DEFAULT CAST(0 AS bit);
    ALTER TABLE [Pago] ADD [FechaRegistroManual] datetime2 NULL;
    ALTER TABLE [Pago] ADD [MetodoPago] nvarchar(40) NOT NULL DEFAULT N'Flow';
    ALTER TABLE [Pago] ADD [ObservacionManual] nvarchar(500) NULL;
    ALTER TABLE [Pago] ADD [ReferenciaManual] nvarchar(100) NULL;
    ALTER TABLE [Pago] ADD [RegistradoPorUserId] nvarchar(128) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508143000_AddManualPayments'
)
BEGIN
    CREATE INDEX [IX_Pago_EsManual] ON [Pago] ([EsManual]);
    CREATE INDEX [IX_Pago_MetodoPago] ON [Pago] ([MetodoPago]);
    CREATE INDEX [IX_Pago_RegistradoPorUserId] ON [Pago] ([RegistradoPorUserId]);
    ALTER TABLE [Pago] ADD CONSTRAINT [FK_Pago_RegistradoPor] FOREIGN KEY ([RegistradoPorUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508143000_AddManualPayments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508143000_AddManualPayments', N'10.0.7');
END;
COMMIT;
GO
