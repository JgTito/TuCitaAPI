using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUsuarioPerfilAndUbicaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pais",
                columns: table => new
                {
                    IdPais = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CodigoIso2 = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pais", x => x.IdPais);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioConsentimiento",
                columns: table => new
                {
                    IdUsuarioConsentimiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuarioPerfil = table.Column<int>(type: "int", nullable: false),
                    AceptaTerminos = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FechaAceptacionTerminos = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AceptaMarketing = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioConsentimiento", x => x.IdUsuarioConsentimiento);
                    table.ForeignKey(
                        name: "FK_UsuarioConsentimiento_UsuarioPerfil_IdUsuarioPerfil",
                        column: x => x.IdUsuarioPerfil,
                        principalTable: "UsuarioPerfil",
                        principalColumn: "IdUsuarioPerfil",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioContacto",
                columns: table => new
                {
                    IdUsuarioContacto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuarioPerfil = table.Column<int>(type: "int", nullable: false),
                    TelefonoAlternativo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioContacto", x => x.IdUsuarioContacto);
                    table.ForeignKey(
                        name: "FK_UsuarioContacto_UsuarioPerfil_IdUsuarioPerfil",
                        column: x => x.IdUsuarioPerfil,
                        principalTable: "UsuarioPerfil",
                        principalColumn: "IdUsuarioPerfil",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioSeguridad",
                columns: table => new
                {
                    IdUsuarioSeguridad = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuarioPerfil = table.Column<int>(type: "int", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DebeCambiarPassword = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FechaUltimoCambioPassword = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaUltimoLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpUltimoLogin = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgentUltimoLogin = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioSeguridad", x => x.IdUsuarioSeguridad);
                    table.ForeignKey(
                        name: "FK_UsuarioSeguridad_UsuarioPerfil_IdUsuarioPerfil",
                        column: x => x.IdUsuarioPerfil,
                        principalTable: "UsuarioPerfil",
                        principalColumn: "IdUsuarioPerfil",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ciudad",
                columns: table => new
                {
                    IdCiudad = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPais = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ciudad", x => x.IdCiudad);
                    table.ForeignKey(
                        name: "FK_Ciudad_Pais",
                        column: x => x.IdPais,
                        principalTable: "Pais",
                        principalColumn: "IdPais",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comuna",
                columns: table => new
                {
                    IdComuna = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCiudad = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comuna", x => x.IdComuna);
                    table.ForeignKey(
                        name: "FK_Comuna_Ciudad",
                        column: x => x.IdCiudad,
                        principalTable: "Ciudad",
                        principalColumn: "IdCiudad",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioDireccion",
                columns: table => new
                {
                    IdUsuarioDireccion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuarioPerfil = table.Column<int>(type: "int", nullable: false),
                    IdComuna = table.Column<int>(type: "int", nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioDireccion", x => x.IdUsuarioDireccion);
                    table.ForeignKey(
                        name: "FK_UsuarioDireccion_Comuna_IdComuna",
                        column: x => x.IdComuna,
                        principalTable: "Comuna",
                        principalColumn: "IdComuna",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioDireccion_UsuarioPerfil_IdUsuarioPerfil",
                        column: x => x.IdUsuarioPerfil,
                        principalTable: "UsuarioPerfil",
                        principalColumn: "IdUsuarioPerfil",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ciudad_IdPais",
                table: "Ciudad",
                column: "IdPais");

            migrationBuilder.CreateIndex(
                name: "UQ_Ciudad_Pais_Nombre",
                table: "Ciudad",
                columns: new[] { "IdPais", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comuna_IdCiudad",
                table: "Comuna",
                column: "IdCiudad");

            migrationBuilder.CreateIndex(
                name: "UQ_Comuna_Ciudad_Nombre",
                table: "Comuna",
                columns: new[] { "IdCiudad", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Pais_CodigoIso2",
                table: "Pais",
                column: "CodigoIso2",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Pais_Nombre",
                table: "Pais",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UsuarioConsentimiento_IdUsuarioPerfil",
                table: "UsuarioConsentimiento",
                column: "IdUsuarioPerfil",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UsuarioContacto_IdUsuarioPerfil",
                table: "UsuarioContacto",
                column: "IdUsuarioPerfil",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioDireccion_IdComuna",
                table: "UsuarioDireccion",
                column: "IdComuna");

            migrationBuilder.CreateIndex(
                name: "UQ_UsuarioDireccion_IdUsuarioPerfil",
                table: "UsuarioDireccion",
                column: "IdUsuarioPerfil",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UsuarioSeguridad_IdUsuarioPerfil",
                table: "UsuarioSeguridad",
                column: "IdUsuarioPerfil",
                unique: true);

            migrationBuilder.Sql("""
                DECLARE @PaisId int;

                IF NOT EXISTS (SELECT 1 FROM Pais WHERE CodigoIso2 = N'CL')
                BEGIN
                    INSERT INTO Pais (Nombre, CodigoIso2, Activo)
                    VALUES (N'Chile', N'CL', 1);
                END
                ELSE
                BEGIN
                    UPDATE Pais SET Nombre = N'Chile', Activo = 1 WHERE CodigoIso2 = N'CL';
                END

                SELECT @PaisId = IdPais FROM Pais WHERE CodigoIso2 = N'CL';

                DECLARE @Ciudades TABLE (Nombre nvarchar(100) NOT NULL);
                INSERT INTO @Ciudades (Nombre)
                VALUES
                    (N'Santiago'),
                    (N'Valparaiso'),
                    (N'Concepcion'),
                    (N'La Serena'),
                    (N'Antofagasta'),
                    (N'Temuco'),
                    (N'Puerto Montt');

                MERGE Ciudad AS target
                USING @Ciudades AS source
                    ON target.IdPais = @PaisId AND target.Nombre = source.Nombre
                WHEN MATCHED THEN
                    UPDATE SET Activo = 1
                WHEN NOT MATCHED THEN
                    INSERT (IdPais, Nombre, Activo)
                    VALUES (@PaisId, source.Nombre, 1);

                DECLARE @Comunas TABLE (Ciudad nvarchar(100) NOT NULL, Nombre nvarchar(100) NOT NULL);
                INSERT INTO @Comunas (Ciudad, Nombre)
                VALUES
                    (N'Santiago', N'Cerrillos'),
                    (N'Santiago', N'Cerro Navia'),
                    (N'Santiago', N'Conchali'),
                    (N'Santiago', N'El Bosque'),
                    (N'Santiago', N'Estacion Central'),
                    (N'Santiago', N'Huechuraba'),
                    (N'Santiago', N'Independencia'),
                    (N'Santiago', N'La Cisterna'),
                    (N'Santiago', N'La Florida'),
                    (N'Santiago', N'La Pintana'),
                    (N'Santiago', N'La Reina'),
                    (N'Santiago', N'Las Condes'),
                    (N'Santiago', N'Lo Barnechea'),
                    (N'Santiago', N'Lo Espejo'),
                    (N'Santiago', N'Lo Prado'),
                    (N'Santiago', N'Macul'),
                    (N'Santiago', N'Maipu'),
                    (N'Santiago', N'Nunoa'),
                    (N'Santiago', N'Pedro Aguirre Cerda'),
                    (N'Santiago', N'Penalolen'),
                    (N'Santiago', N'Providencia'),
                    (N'Santiago', N'Pudahuel'),
                    (N'Santiago', N'Quilicura'),
                    (N'Santiago', N'Quinta Normal'),
                    (N'Santiago', N'Recoleta'),
                    (N'Santiago', N'Renca'),
                    (N'Santiago', N'San Joaquin'),
                    (N'Santiago', N'San Miguel'),
                    (N'Santiago', N'San Ramon'),
                    (N'Santiago', N'Santiago'),
                    (N'Santiago', N'Vitacura'),
                    (N'Valparaiso', N'Valparaiso'),
                    (N'Valparaiso', N'Vina del Mar'),
                    (N'Valparaiso', N'Concon'),
                    (N'Valparaiso', N'Quilpue'),
                    (N'Valparaiso', N'Villa Alemana'),
                    (N'Valparaiso', N'San Antonio'),
                    (N'Valparaiso', N'Quillota'),
                    (N'Concepcion', N'Concepcion'),
                    (N'Concepcion', N'Talcahuano'),
                    (N'Concepcion', N'San Pedro de la Paz'),
                    (N'Concepcion', N'Chiguayante'),
                    (N'Concepcion', N'Hualpen'),
                    (N'Concepcion', N'Coronel'),
                    (N'Concepcion', N'Lota'),
                    (N'La Serena', N'La Serena'),
                    (N'La Serena', N'Coquimbo'),
                    (N'La Serena', N'Ovalle'),
                    (N'Antofagasta', N'Antofagasta'),
                    (N'Antofagasta', N'Calama'),
                    (N'Antofagasta', N'Tocopilla'),
                    (N'Temuco', N'Temuco'),
                    (N'Temuco', N'Padre Las Casas'),
                    (N'Temuco', N'Villarrica'),
                    (N'Temuco', N'Pucon'),
                    (N'Puerto Montt', N'Puerto Montt'),
                    (N'Puerto Montt', N'Puerto Varas'),
                    (N'Puerto Montt', N'Osorno'),
                    (N'Puerto Montt', N'Castro');

                MERGE Comuna AS target
                USING (
                    SELECT ciudad.IdCiudad, source.Nombre
                    FROM @Comunas AS source
                    INNER JOIN Ciudad AS ciudad
                        ON ciudad.IdPais = @PaisId AND ciudad.Nombre = source.Ciudad
                ) AS source
                    ON target.IdCiudad = source.IdCiudad AND target.Nombre = source.Nombre
                WHEN MATCHED THEN
                    UPDATE SET Activo = 1
                WHEN NOT MATCHED THEN
                    INSERT (IdCiudad, Nombre, Activo)
                    VALUES (source.IdCiudad, source.Nombre, 1);
                """);

            migrationBuilder.Sql("""
                INSERT INTO UsuarioContacto (IdUsuarioPerfil, TelefonoAlternativo, FechaCreacion, FechaActualizacion)
                SELECT IdUsuarioPerfil, TelefonoAlternativo, SYSUTCDATETIME(), FechaActualizacion
                FROM UsuarioPerfil;
                """);

            migrationBuilder.Sql("""
                INSERT INTO UsuarioDireccion (IdUsuarioPerfil, IdComuna, Direccion, FechaCreacion, FechaActualizacion)
                SELECT
                    perfil.IdUsuarioPerfil,
                    comuna.IdComuna,
                    perfil.Direccion,
                    SYSUTCDATETIME(),
                    perfil.FechaActualizacion
                FROM UsuarioPerfil AS perfil
                OUTER APPLY (
                    SELECT TOP 1 comunaLookup.IdComuna
                    FROM Comuna AS comunaLookup
                    INNER JOIN Ciudad AS ciudadLookup ON ciudadLookup.IdCiudad = comunaLookup.IdCiudad
                    INNER JOIN Pais AS paisLookup ON paisLookup.IdPais = ciudadLookup.IdPais
                    WHERE
                        perfil.Comuna IS NOT NULL
                        AND comunaLookup.Nombre = perfil.Comuna
                        AND (perfil.Ciudad IS NULL OR ciudadLookup.Nombre = perfil.Ciudad)
                        AND (
                            perfil.Pais IS NULL OR
                            paisLookup.Nombre = perfil.Pais OR
                            paisLookup.CodigoIso2 = perfil.Pais
                        )
                    ORDER BY comunaLookup.Nombre
                ) AS comuna;
                """);

            migrationBuilder.Sql("""
                INSERT INTO UsuarioConsentimiento (
                    IdUsuarioPerfil,
                    AceptaTerminos,
                    FechaAceptacionTerminos,
                    AceptaMarketing,
                    FechaCreacion,
                    FechaActualizacion)
                SELECT
                    IdUsuarioPerfil,
                    AceptaTerminos,
                    FechaAceptacionTerminos,
                    AceptaMarketing,
                    SYSUTCDATETIME(),
                    FechaActualizacion
                FROM UsuarioPerfil;
                """);

            migrationBuilder.Sql("""
                INSERT INTO UsuarioSeguridad (
                    IdUsuarioPerfil,
                    UltimoAcceso,
                    DebeCambiarPassword,
                    FechaUltimoCambioPassword,
                    FechaUltimoLogin,
                    IpUltimoLogin,
                    UserAgentUltimoLogin,
                    FechaCreacion,
                    FechaActualizacion)
                SELECT
                    IdUsuarioPerfil,
                    UltimoAcceso,
                    DebeCambiarPassword,
                    FechaUltimoCambioPassword,
                    FechaUltimoLogin,
                    IpUltimoLogin,
                    UserAgentUltimoLogin,
                    SYSUTCDATETIME(),
                    FechaActualizacion
                FROM UsuarioPerfil;
                """);

            migrationBuilder.DropColumn(
                name: "AceptaMarketing",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "AceptaTerminos",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "Ciudad",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "Comuna",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "DebeCambiarPassword",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "FechaAceptacionTerminos",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "FechaUltimoCambioPassword",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "FechaUltimoLogin",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "IpUltimoLogin",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "Pais",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "TelefonoAlternativo",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "UltimoAcceso",
                table: "UsuarioPerfil");

            migrationBuilder.DropColumn(
                name: "UserAgentUltimoLogin",
                table: "UsuarioPerfil");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AceptaMarketing",
                table: "UsuarioPerfil",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AceptaTerminos",
                table: "UsuarioPerfil",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Ciudad",
                table: "UsuarioPerfil",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comuna",
                table: "UsuarioPerfil",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DebeCambiarPassword",
                table: "UsuarioPerfil",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "UsuarioPerfil",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAceptacionTerminos",
                table: "UsuarioPerfil",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimoCambioPassword",
                table: "UsuarioPerfil",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimoLogin",
                table: "UsuarioPerfil",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpUltimoLogin",
                table: "UsuarioPerfil",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pais",
                table: "UsuarioPerfil",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoAlternativo",
                table: "UsuarioPerfil",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoAcceso",
                table: "UsuarioPerfil",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgentUltimoLogin",
                table: "UsuarioPerfil",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE perfil
                SET
                    perfil.TelefonoAlternativo = contacto.TelefonoAlternativo
                FROM UsuarioPerfil AS perfil
                INNER JOIN UsuarioContacto AS contacto
                    ON contacto.IdUsuarioPerfil = perfil.IdUsuarioPerfil;
                """);

            migrationBuilder.Sql("""
                UPDATE perfil
                SET
                    perfil.Direccion = direccion.Direccion,
                    perfil.Comuna = comuna.Nombre,
                    perfil.Ciudad = ciudad.Nombre,
                    perfil.Pais = pais.Nombre
                FROM UsuarioPerfil AS perfil
                INNER JOIN UsuarioDireccion AS direccion
                    ON direccion.IdUsuarioPerfil = perfil.IdUsuarioPerfil
                LEFT JOIN Comuna AS comuna
                    ON comuna.IdComuna = direccion.IdComuna
                LEFT JOIN Ciudad AS ciudad
                    ON ciudad.IdCiudad = comuna.IdCiudad
                LEFT JOIN Pais AS pais
                    ON pais.IdPais = ciudad.IdPais;
                """);

            migrationBuilder.Sql("""
                UPDATE perfil
                SET
                    perfil.AceptaTerminos = consentimiento.AceptaTerminos,
                    perfil.FechaAceptacionTerminos = consentimiento.FechaAceptacionTerminos,
                    perfil.AceptaMarketing = consentimiento.AceptaMarketing
                FROM UsuarioPerfil AS perfil
                INNER JOIN UsuarioConsentimiento AS consentimiento
                    ON consentimiento.IdUsuarioPerfil = perfil.IdUsuarioPerfil;
                """);

            migrationBuilder.Sql("""
                UPDATE perfil
                SET
                    perfil.UltimoAcceso = seguridad.UltimoAcceso,
                    perfil.DebeCambiarPassword = seguridad.DebeCambiarPassword,
                    perfil.FechaUltimoCambioPassword = seguridad.FechaUltimoCambioPassword,
                    perfil.FechaUltimoLogin = seguridad.FechaUltimoLogin,
                    perfil.IpUltimoLogin = seguridad.IpUltimoLogin,
                    perfil.UserAgentUltimoLogin = seguridad.UserAgentUltimoLogin
                FROM UsuarioPerfil AS perfil
                INNER JOIN UsuarioSeguridad AS seguridad
                    ON seguridad.IdUsuarioPerfil = perfil.IdUsuarioPerfil;
                """);

            migrationBuilder.DropTable(
                name: "UsuarioConsentimiento");

            migrationBuilder.DropTable(
                name: "UsuarioContacto");

            migrationBuilder.DropTable(
                name: "UsuarioDireccion");

            migrationBuilder.DropTable(
                name: "UsuarioSeguridad");

            migrationBuilder.DropTable(
                name: "Comuna");

            migrationBuilder.DropTable(
                name: "Ciudad");

            migrationBuilder.DropTable(
                name: "Pais");
        }
    }
}
