using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class CampoReservaPorServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_CampoReserva_Negocio_NombreInterno",
                table: "CampoReserva");

            migrationBuilder.AddColumn<int>(
                name: "IdServicio",
                table: "CampoReserva",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampoReserva_IdServicio",
                table: "CampoReserva",
                column: "IdServicio");

            migrationBuilder.CreateIndex(
                name: "UQ_CampoReserva_Negocio_NombreInterno_Global",
                table: "CampoReserva",
                columns: new[] { "IdNegocio", "NombreInterno" },
                unique: true,
                filter: "[IdServicio] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_CampoReserva_Negocio_Servicio_NombreInterno",
                table: "CampoReserva",
                columns: new[] { "IdNegocio", "IdServicio", "NombreInterno" },
                unique: true,
                filter: "[IdServicio] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CampoReserva_Servicio",
                table: "CampoReserva",
                columns: new[] { "IdNegocio", "IdServicio" },
                principalTable: "Servicio",
                principalColumns: new[] { "IdNegocio", "IdServicio" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CampoReserva_Servicio",
                table: "CampoReserva");

            migrationBuilder.DropIndex(
                name: "IX_CampoReserva_IdServicio",
                table: "CampoReserva");

            migrationBuilder.DropIndex(
                name: "UQ_CampoReserva_Negocio_NombreInterno_Global",
                table: "CampoReserva");

            migrationBuilder.DropIndex(
                name: "UQ_CampoReserva_Negocio_Servicio_NombreInterno",
                table: "CampoReserva");

            migrationBuilder.DropColumn(
                name: "IdServicio",
                table: "CampoReserva");

            migrationBuilder.CreateIndex(
                name: "UQ_CampoReserva_Negocio_NombreInterno",
                table: "CampoReserva",
                columns: new[] { "IdNegocio", "NombreInterno" },
                unique: true);
        }
    }
}
