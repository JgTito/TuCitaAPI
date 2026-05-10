BEGIN TRANSACTION;
ALTER TABLE [ResenaNegocio] ADD [EsAlertaOperativa] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [ResenaNegocio] ADD [FechaAlertaOperativa] datetime2 NULL;

ALTER TABLE [ResenaNegocio] ADD [MotivoAlertaOperativa] nvarchar(300) NULL;

ALTER TABLE [Notificacion] ADD [IdResenaNegocio] int NULL;

ALTER TABLE [ConfiguracionResenaNegocio] ADD [PuntuacionMaximaAlertaOperativa] tinyint NOT NULL DEFAULT CAST(2 AS tinyint);

CREATE INDEX [IX_ResenaNegocio_AlertaOperativa] ON [ResenaNegocio] ([IdNegocio], [EsAlertaOperativa], [FechaAlertaOperativa]);

CREATE INDEX [IX_Notificacion_IdResenaNegocio] ON [Notificacion] ([IdResenaNegocio]);

ALTER TABLE [ConfiguracionResenaNegocio] ADD CONSTRAINT [CK_ConfiguracionResenaNegocio_PuntuacionAlerta] CHECK ([PuntuacionMaximaAlertaOperativa] BETWEEN 1 AND 5);

ALTER TABLE [Notificacion] ADD CONSTRAINT [FK_Notificacion_ResenaNegocio] FOREIGN KEY ([IdResenaNegocio]) REFERENCES [ResenaNegocio] ([IdResenaNegocio]) ON DELETE NO ACTION;

UPDATE ResenaNegocio
SET
    EsAlertaOperativa = 1,
    FechaAlertaOperativa = COALESCE(FechaActualizacion, FechaCreacion, SYSDATETIME()),
    MotivoAlertaOperativa = N'Puntuacion historica menor o igual al umbral operativo 2/5.'
WHERE Puntuacion <= 2;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260510020532_AddResenaNotificacionesAlertas', N'10.0.7');

COMMIT;
GO

