using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TuCita.Infrastucture.Persistence;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ReservaFlowDbContext))]
    [Migration("20260508143000_AddManualPayments")]
    public partial class AddManualPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsManual",
                table: "Pago",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRegistroManual",
                table: "Pago",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "Pago",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Flow");

            migrationBuilder.AddColumn<string>(
                name: "ObservacionManual",
                table: "Pago",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenciaManual",
                table: "Pago",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistradoPorUserId",
                table: "Pago",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pago_EsManual",
                table: "Pago",
                column: "EsManual");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_MetodoPago",
                table: "Pago",
                column: "MetodoPago");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_RegistradoPorUserId",
                table: "Pago",
                column: "RegistradoPorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pago_RegistradoPor",
                table: "Pago",
                column: "RegistradoPorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pago_RegistradoPor",
                table: "Pago");

            migrationBuilder.DropIndex(
                name: "IX_Pago_EsManual",
                table: "Pago");

            migrationBuilder.DropIndex(
                name: "IX_Pago_MetodoPago",
                table: "Pago");

            migrationBuilder.DropIndex(
                name: "IX_Pago_RegistradoPorUserId",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "EsManual",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "FechaRegistroManual",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "ObservacionManual",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "ReferenciaManual",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "RegistradoPorUserId",
                table: "Pago");
        }
    }
}
