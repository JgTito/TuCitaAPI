using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionResenasNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionResenaNegocio",
                columns: table => new
                {
                    IdConfiguracionResenaNegocio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    ResenasActivas = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AutoaprobarResenas = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DiasMaximosParaCalificar = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    PermitirRespuestaNegocio = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MostrarResenasPublicas = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionResenaNegocio", x => x.IdConfiguracionResenaNegocio);
                    table.CheckConstraint("CK_ConfiguracionResenaNegocio_DiasMaximos", "[DiasMaximosParaCalificar] BETWEEN 1 AND 365");
                    table.ForeignKey(
                        name: "FK_ConfiguracionResenaNegocio_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_ConfiguracionResenaNegocio_Negocio",
                table: "ConfiguracionResenaNegocio",
                column: "IdNegocio",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionResenaNegocio");
        }
    }
}
