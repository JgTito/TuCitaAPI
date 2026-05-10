BEGIN TRANSACTION;
CREATE TABLE [ConfiguracionResenaNegocio] (
    [IdConfiguracionResenaNegocio] int NOT NULL IDENTITY,
    [IdNegocio] int NOT NULL,
    [ResenasActivas] bit NOT NULL DEFAULT CAST(1 AS bit),
    [AutoaprobarResenas] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DiasMaximosParaCalificar] int NOT NULL DEFAULT 15,
    [PermitirRespuestaNegocio] bit NOT NULL DEFAULT CAST(1 AS bit),
    [MostrarResenasPublicas] bit NOT NULL DEFAULT CAST(1 AS bit),
    [FechaCreacion] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [FechaActualizacion] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_ConfiguracionResenaNegocio] PRIMARY KEY ([IdConfiguracionResenaNegocio]),
    CONSTRAINT [CK_ConfiguracionResenaNegocio_DiasMaximos] CHECK ([DiasMaximosParaCalificar] BETWEEN 1 AND 365),
    CONSTRAINT [FK_ConfiguracionResenaNegocio_Negocio] FOREIGN KEY ([IdNegocio]) REFERENCES [Negocio] ([IdNegocio]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [UQ_ConfiguracionResenaNegocio_Negocio] ON [ConfiguracionResenaNegocio] ([IdNegocio]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260510015452_AddConfiguracionResenasNegocio', N'10.0.7');

COMMIT;
GO

