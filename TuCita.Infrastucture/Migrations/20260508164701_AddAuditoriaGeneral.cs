using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaGeneral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditoriaEvento",
                columns: table => new
                {
                    IdAuditoriaEvento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Entidad = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntidadId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ValoresAnterioresJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValoresNuevosJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CambiosJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaEvento", x => x.IdAuditoriaEvento);
                    table.ForeignKey(
                        name: "FK_AuditoriaEvento_AspNetUsers",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditoriaEvento_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaEvento_Categoria_Accion",
                table: "AuditoriaEvento",
                columns: new[] { "Categoria", "Accion" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaEvento_Entidad_EntidadId",
                table: "AuditoriaEvento",
                columns: new[] { "Entidad", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaEvento_IdNegocio_FechaCreacion",
                table: "AuditoriaEvento",
                columns: new[] { "IdNegocio", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaEvento_UserId",
                table: "AuditoriaEvento",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriaEvento");
        }
    }
}
