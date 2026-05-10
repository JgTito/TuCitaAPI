BEGIN TRANSACTION;
ALTER TABLE [Pago] ADD CONSTRAINT [CK_Pago_MontoDevuelto] CHECK ([MontoDevuelto] >= 0 AND [MontoDevuelto] <= [Monto]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260508162108_AddPagoMontoDevueltoConstraint', N'10.0.7');

COMMIT;
GO

