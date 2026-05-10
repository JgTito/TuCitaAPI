BEGIN TRANSACTION;
CREATE TABLE [ResenaNegocio] (
    [IdResenaNegocio] int NOT NULL IDENTITY,
    [IdNegocio] int NOT NULL,
    [IdCita] int NOT NULL,
    [IdCliente] int NOT NULL,
    [UserId] nvarchar(128) NULL,
    [IdServicio] int NOT NULL,
    [IdPrestador] int NULL,
    [Puntuacion] tinyint NOT NULL,
    [Comentario] nvarchar(1500) NULL,
    [Estado] nvarchar(30) NOT NULL,
    [EsVisiblePublicamente] bit NOT NULL,
    [FechaCreacion] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [FechaActualizacion] datetime2 NULL,
    [FechaPublicacion] datetime2 NULL,
    [ModeradoPorUserId] nvarchar(128) NULL,
    [FechaModeracion] datetime2 NULL,
    [MotivoModeracion] nvarchar(300) NULL,
    [RespuestaNegocio] nvarchar(1000) NULL,
    [RespondidoPorUserId] nvarchar(128) NULL,
    [FechaRespuesta] datetime2 NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    [ClienteNombreSnapshot] nvarchar(150) NOT NULL,
    [ServicioNombreSnapshot] nvarchar(150) NOT NULL,
    [PrestadorNombreSnapshot] nvarchar(150) NULL,
    CONSTRAINT [PK_ResenaNegocio] PRIMARY KEY ([IdResenaNegocio]),
    CONSTRAINT [CK_ResenaNegocio_Estado] CHECK ([Estado] IN ('Pendiente', 'Aprobada', 'Rechazada', 'Oculta')),
    CONSTRAINT [CK_ResenaNegocio_Puntuacion] CHECK ([Puntuacion] BETWEEN 1 AND 5),
    CONSTRAINT [FK_ResenaNegocio_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ResenaNegocio_Cita] FOREIGN KEY ([IdNegocio], [IdCita]) REFERENCES [Cita] ([IdNegocio], [IdCita]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ResenaNegocio_Cliente] FOREIGN KEY ([IdNegocio], [IdCliente]) REFERENCES [Cliente] ([IdNegocio], [IdCliente]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ResenaNegocio_ModeradoPor] FOREIGN KEY ([ModeradoPorUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ResenaNegocio_Negocio] FOREIGN KEY ([IdNegocio]) REFERENCES [Negocio] ([IdNegocio]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ResenaNegocio_Prestador] FOREIGN KEY ([IdNegocio], [IdPrestador]) REFERENCES [Prestador] ([IdNegocio], [IdPrestador]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ResenaNegocio_RespondidoPor] FOREIGN KEY ([RespondidoPorUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ResenaNegocio_Servicio] FOREIGN KEY ([IdNegocio], [IdServicio]) REFERENCES [Servicio] ([IdNegocio], [IdServicio]) ON DELETE NO ACTION
);

CREATE TABLE [SolicitudResena] (
    [IdSolicitudResena] int NOT NULL IDENTITY,
    [IdNegocio] int NOT NULL,
    [IdCita] int NOT NULL,
    [IdCliente] int NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [NormalizedEmail] nvarchar(256) NOT NULL,
    [TokenHash] nvarchar(500) NOT NULL,
    [Estado] nvarchar(30) NOT NULL,
    [FechaCreacion] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [FechaExpiracion] datetime2 NOT NULL,
    [FechaUso] datetime2 NULL,
    [FechaCancelacion] datetime2 NULL,
    CONSTRAINT [PK_SolicitudResena] PRIMARY KEY ([IdSolicitudResena]),
    CONSTRAINT [CK_SolicitudResena_Estado] CHECK ([Estado] IN ('Pendiente', 'Usada', 'Expirada', 'Cancelada')),
    CONSTRAINT [FK_SolicitudResena_Cita] FOREIGN KEY ([IdNegocio], [IdCita]) REFERENCES [Cita] ([IdNegocio], [IdCita]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SolicitudResena_Cliente] FOREIGN KEY ([IdNegocio], [IdCliente]) REFERENCES [Cliente] ([IdNegocio], [IdCliente]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SolicitudResena_Negocio] FOREIGN KEY ([IdNegocio]) REFERENCES [Negocio] ([IdNegocio]) ON DELETE NO ACTION
);

CREATE INDEX [IX_ResenaNegocio_IdNegocio_IdCliente] ON [ResenaNegocio] ([IdNegocio], [IdCliente]);

CREATE INDEX [IX_ResenaNegocio_IdNegocio_IdPrestador] ON [ResenaNegocio] ([IdNegocio], [IdPrestador]);

CREATE INDEX [IX_ResenaNegocio_IdNegocio_IdServicio] ON [ResenaNegocio] ([IdNegocio], [IdServicio]);

CREATE INDEX [IX_ResenaNegocio_IdPrestador] ON [ResenaNegocio] ([IdPrestador]);

CREATE INDEX [IX_ResenaNegocio_IdServicio] ON [ResenaNegocio] ([IdServicio]);

CREATE INDEX [IX_ResenaNegocio_ModeradoPorUserId] ON [ResenaNegocio] ([ModeradoPorUserId]);

CREATE INDEX [IX_ResenaNegocio_Negocio_Estado_Fecha] ON [ResenaNegocio] ([IdNegocio], [Estado], [FechaCreacion]);

CREATE INDEX [IX_ResenaNegocio_Publicas] ON [ResenaNegocio] ([IdNegocio], [EsVisiblePublicamente], [FechaPublicacion]);

CREATE INDEX [IX_ResenaNegocio_RespondidoPorUserId] ON [ResenaNegocio] ([RespondidoPorUserId]);

CREATE INDEX [IX_ResenaNegocio_UserId] ON [ResenaNegocio] ([UserId]);

CREATE UNIQUE INDEX [UQ_ResenaNegocio_Negocio_Cita] ON [ResenaNegocio] ([IdNegocio], [IdCita]);

CREATE INDEX [IX_SolicitudResena_Estado_FechaExpiracion] ON [SolicitudResena] ([Estado], [FechaExpiracion]);

CREATE INDEX [IX_SolicitudResena_IdNegocio_IdCliente] ON [SolicitudResena] ([IdNegocio], [IdCliente]);

CREATE INDEX [IX_SolicitudResena_NormalizedEmail] ON [SolicitudResena] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UQ_SolicitudResena_Pendiente_Negocio_Cita] ON [SolicitudResena] ([IdNegocio], [IdCita]) WHERE [Estado] = 'Pendiente';

CREATE UNIQUE INDEX [UQ_SolicitudResena_TokenHash] ON [SolicitudResena] ([TokenHash]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260510013649_AddResenasNegocio', N'10.0.7');

COMMIT;
GO

