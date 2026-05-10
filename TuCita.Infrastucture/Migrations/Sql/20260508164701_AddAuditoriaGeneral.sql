BEGIN TRANSACTION;
CREATE TABLE [AuditoriaEvento] (
    [IdAuditoriaEvento] int NOT NULL IDENTITY,
    [IdNegocio] int NULL,
    [UserId] nvarchar(128) NULL,
    [Categoria] nvarchar(80) NOT NULL,
    [Accion] nvarchar(80) NOT NULL,
    [Entidad] nvarchar(120) NOT NULL,
    [EntidadId] nvarchar(128) NULL,
    [Descripcion] nvarchar(500) NOT NULL,
    [ValoresAnterioresJson] nvarchar(max) NULL,
    [ValoresNuevosJson] nvarchar(max) NULL,
    [CambiosJson] nvarchar(max) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [FechaCreacion] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_AuditoriaEvento] PRIMARY KEY ([IdAuditoriaEvento]),
    CONSTRAINT [FK_AuditoriaEvento_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AuditoriaEvento_Negocio] FOREIGN KEY ([IdNegocio]) REFERENCES [Negocio] ([IdNegocio]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AuditoriaEvento_Categoria_Accion] ON [AuditoriaEvento] ([Categoria], [Accion]);

CREATE INDEX [IX_AuditoriaEvento_Entidad_EntidadId] ON [AuditoriaEvento] ([Entidad], [EntidadId]);

CREATE INDEX [IX_AuditoriaEvento_IdNegocio_FechaCreacion] ON [AuditoriaEvento] ([IdNegocio], [FechaCreacion]);

CREATE INDEX [IX_AuditoriaEvento_UserId] ON [AuditoriaEvento] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260508164701_AddAuditoriaGeneral', N'10.0.7');

COMMIT;
GO

