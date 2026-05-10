using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class AddResenaNotificacionesAlertas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsAlertaOperativa",
                table: "ResenaNegocio",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAlertaOperativa",
                table: "ResenaNegocio",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAlertaOperativa",
                table: "ResenaNegocio",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdResenaNegocio",
                table: "Notificacion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PuntuacionMaximaAlertaOperativa",
                table: "ConfiguracionResenaNegocio",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)2);

            migrationBuilder.CreateIndex(
                name: "IX_ResenaNegocio_AlertaOperativa",
                table: "ResenaNegocio",
                columns: new[] { "IdNegocio", "EsAlertaOperativa", "FechaAlertaOperativa" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacion_IdResenaNegocio",
                table: "Notificacion",
                column: "IdResenaNegocio");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ConfiguracionResenaNegocio_PuntuacionAlerta",
                table: "ConfiguracionResenaNegocio",
                sql: "[PuntuacionMaximaAlertaOperativa] BETWEEN 1 AND 5");

            migrationBuilder.AddForeignKey(
                name: "FK_Notificacion_ResenaNegocio",
                table: "Notificacion",
                column: "IdResenaNegocio",
                principalTable: "ResenaNegocio",
                principalColumn: "IdResenaNegocio",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE ResenaNegocio
                SET
                    EsAlertaOperativa = 1,
                    FechaAlertaOperativa = COALESCE(FechaActualizacion, FechaCreacion, SYSDATETIME()),
                    MotivoAlertaOperativa = N'Puntuacion historica menor o igual al umbral operativo 2/5.'
                WHERE Puntuacion <= 2;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notificacion_ResenaNegocio",
                table: "Notificacion");

            migrationBuilder.DropIndex(
                name: "IX_ResenaNegocio_AlertaOperativa",
                table: "ResenaNegocio");

            migrationBuilder.DropIndex(
                name: "IX_Notificacion_IdResenaNegocio",
                table: "Notificacion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ConfiguracionResenaNegocio_PuntuacionAlerta",
                table: "ConfiguracionResenaNegocio");

            migrationBuilder.DropColumn(
                name: "EsAlertaOperativa",
                table: "ResenaNegocio");

            migrationBuilder.DropColumn(
                name: "FechaAlertaOperativa",
                table: "ResenaNegocio");

            migrationBuilder.DropColumn(
                name: "MotivoAlertaOperativa",
                table: "ResenaNegocio");

            migrationBuilder.DropColumn(
                name: "IdResenaNegocio",
                table: "Notificacion");

            migrationBuilder.DropColumn(
                name: "PuntuacionMaximaAlertaOperativa",
                table: "ConfiguracionResenaNegocio");
        }
    }
}
