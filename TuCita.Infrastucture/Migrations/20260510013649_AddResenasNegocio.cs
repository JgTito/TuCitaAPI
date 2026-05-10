using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class AddResenasNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResenaNegocio",
                columns: table => new
                {
                    IdResenaNegocio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdCita = table.Column<int>(type: "int", nullable: false),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IdServicio = table.Column<int>(type: "int", nullable: false),
                    IdPrestador = table.Column<int>(type: "int", nullable: true),
                    Puntuacion = table.Column<byte>(type: "tinyint", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EsVisiblePublicamente = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaPublicacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModeradoPorUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FechaModeracion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoModeracion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RespuestaNegocio = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RespondidoPorUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FechaRespuesta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ClienteNombreSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ServicioNombreSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PrestadorNombreSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResenaNegocio", x => x.IdResenaNegocio);
                    table.CheckConstraint("CK_ResenaNegocio_Estado", "[Estado] IN ('Pendiente', 'Aprobada', 'Rechazada', 'Oculta')");
                    table.CheckConstraint("CK_ResenaNegocio_Puntuacion", "[Puntuacion] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_ResenaNegocio_AspNetUsers",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResenaNegocio_Cita",
                        columns: x => new { x.IdNegocio, x.IdCita },
                        principalTable: "Cita",
                        principalColumns: new[] { "IdNegocio", "IdCita" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResenaNegocio_Cliente",
                        columns: x => new { x.IdNegocio, x.IdCliente },
                        principalTable: "Cliente",
                        principalColumns: new[] { "IdNegocio", "IdCliente" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResenaNegocio_ModeradoPor",
                        column: x => x.ModeradoPorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResenaNegocio_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResenaNegocio_Prestador",
                        columns: x => new { x.IdNegocio, x.IdPrestador },
                        principalTable: "Prestador",
                        principalColumns: new[] { "IdNegocio", "IdPrestador" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResenaNegocio_RespondidoPor",
                        column: x => x.RespondidoPorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResenaNegocio_Servicio",
                        columns: x => new { x.IdNegocio, x.IdServicio },
                        principalTable: "Servicio",
                        principalColumns: new[] { "IdNegocio", "IdServicio" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudResena",
                columns: table => new
                {
                    IdSolicitudResena = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdCita = table.Column<int>(type: "int", nullable: false),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaUso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCancelacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudResena", x => x.IdSolicitudResena);
                    table.CheckConstraint("CK_SolicitudResena_Estado", "[Estado] IN ('Pendiente', 'Usada', 'Expirada', 'Cancelada')");
                    table.ForeignKey(
                        name: "FK_SolicitudResena_Cita",
                        columns: x => new { x.IdNegocio, x.IdCita },
                        principalTable: "Cita",
                        principalColumns: new[] { "IdNegocio", "IdCita" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudResena_Cliente",
                        columns: x => new { x.IdNegocio, x.IdCliente },
                        principalTable: "Cliente",
                        principalColumns: new[] { "IdNegocio", "IdCliente" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudResena_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResenaNegocio_IdNegocio_IdCliente",
                table: "ResenaNegocio",
                columns: new[] { "IdNegocio", "IdCliente" });

            migrationBuilder.CreateIndex(
                name: "IX_ResenaNegocio_IdNegocio_IdPrestador",
                table: "ResenaNegocio",
                columns: new[] { "IdNegocio", "IdPrestador" });

            migrationBuilder.CreateIndex(
                name: "IX_ResenaNegocio_IdNegocio_IdServicio",
                table: "ResenaNegocio",
                columns: new[] { "IdNegocio", "IdServicio" });

            migrationBuilder.CreateIndex(
                name: "IX_ResenaNegocio_IdPrestador",
                table: "ResenaNegocio",
                column: "IdPrestador");

            migrationBuilder.CreateIndex(
                name: "IX_ResenaNegocio_IdServicio",
                table: "ResenaNegocio",
                column: "IdServicio");

            migrationBuilder.CreateIndex(
                name: "IX_ResenaNegocio_ModeradoPorUserId",
                table: "ResenaNegocio",
                column: "ModeradoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResenaNegocio_Negocio_Estado_Fecha",
                table: "ResenaNegocio",
                columns: new[] { "IdNegocio", "Estado", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_ResenaNegocio_Publicas",
                table: "ResenaNegocio",
                columns: new[] { "IdNegocio", "EsVisiblePublicamente", "FechaPublicacion" });

            migrationBuilder.CreateIndex(
                name: "IX_ResenaNegocio_RespondidoPorUserId",
                table: "ResenaNegocio",
                column: "RespondidoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResenaNegocio_UserId",
                table: "ResenaNegocio",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_ResenaNegocio_Negocio_Cita",
                table: "ResenaNegocio",
                columns: new[] { "IdNegocio", "IdCita" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudResena_Estado_FechaExpiracion",
                table: "SolicitudResena",
                columns: new[] { "Estado", "FechaExpiracion" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudResena_IdNegocio_IdCliente",
                table: "SolicitudResena",
                columns: new[] { "IdNegocio", "IdCliente" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudResena_NormalizedEmail",
                table: "SolicitudResena",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UQ_SolicitudResena_Pendiente_Negocio_Cita",
                table: "SolicitudResena",
                columns: new[] { "IdNegocio", "IdCita" },
                unique: true,
                filter: "[Estado] = 'Pendiente'");

            migrationBuilder.CreateIndex(
                name: "UQ_SolicitudResena_TokenHash",
                table: "SolicitudResena",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResenaNegocio");

            migrationBuilder.DropTable(
                name: "SolicitudResena");
        }
    }
}
