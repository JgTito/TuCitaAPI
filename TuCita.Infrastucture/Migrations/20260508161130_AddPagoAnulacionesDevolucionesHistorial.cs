using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class AddPagoAnulacionesDevolucionesHistorial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                MERGE [dbo].[EstadoPago] AS target
                USING (VALUES
                    (N'Parcialmente devuelto', N'El pago fue confirmado y tiene una devolucion parcial registrada', CAST(0 AS bit), CAST(1 AS bit)),
                    (N'Devuelto', N'El pago fue devuelto completamente', CAST(1 AS bit), CAST(1 AS bit))
                ) AS source ([Nombre], [Descripcion], [EsEstadoFinal], [Activo])
                ON target.[Nombre] = source.[Nombre]
                WHEN MATCHED THEN
                    UPDATE SET
                        [Descripcion] = source.[Descripcion],
                        [EsEstadoFinal] = source.[EsEstadoFinal],
                        [Activo] = source.[Activo]
                WHEN NOT MATCHED THEN
                    INSERT ([Nombre], [Descripcion], [EsEstadoFinal], [Activo])
                    VALUES (source.[Nombre], source.[Descripcion], source.[EsEstadoFinal], source.[Activo]);
                """);

            migrationBuilder.AddColumn<string>(
                name: "AnuladoPorUserId",
                table: "Pago",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAnulacion",
                table: "Pago",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimaDevolucion",
                table: "Pago",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDevuelto",
                table: "Pago",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "Pago",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenciaAnulacion",
                table: "Pago",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PagoHistorial",
                columns: table => new
                {
                    IdPagoHistorial = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPago = table.Column<int>(type: "int", nullable: false),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdCita = table.Column<int>(type: "int", nullable: false),
                    TipoEvento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EstadoAnterior = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    EstadoNuevo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DatosJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoHistorial", x => x.IdPagoHistorial);
                    table.CheckConstraint("CK_PagoHistorial_Monto", "[Monto] IS NULL OR [Monto] >= 0");
                    table.ForeignKey(
                        name: "FK_PagoHistorial_AspNetUsers",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagoHistorial_Cita",
                        columns: x => new { x.IdNegocio, x.IdCita },
                        principalTable: "Cita",
                        principalColumns: new[] { "IdNegocio", "IdCita" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagoHistorial_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagoHistorial_Pago",
                        column: x => x.IdPago,
                        principalTable: "Pago",
                        principalColumn: "IdPago",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO [dbo].[PagoHistorial]
                    ([IdPago], [IdNegocio], [IdCita], [TipoEvento], [EstadoAnterior], [EstadoNuevo], [Monto], [Motivo], [Referencia], [UserId], [DatosJson], [FechaCreacion])
                SELECT
                    pago.[IdPago],
                    pago.[IdNegocio],
                    pago.[IdCita],
                    N'MigracionInicial',
                    NULL,
                    estado.[Nombre],
                    pago.[Monto],
                    N'Historial inicial generado al habilitar auditoria de pagos.',
                    pago.[CommerceOrder],
                    pago.[RegistradoPorUserId],
                    NULL,
                    COALESCE(pago.[FechaActualizacion], pago.[FechaCreacion])
                FROM [dbo].[Pago] AS pago
                INNER JOIN [dbo].[EstadoPago] AS estado
                    ON estado.[IdEstadoPago] = pago.[IdEstadoPago]
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [dbo].[PagoHistorial] AS historial
                    WHERE historial.[IdPago] = pago.[IdPago]
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Pago_AnuladoPorUserId",
                table: "Pago",
                column: "AnuladoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PagoHistorial_FechaCreacion",
                table: "PagoHistorial",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_PagoHistorial_IdNegocio",
                table: "PagoHistorial",
                column: "IdNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_PagoHistorial_IdNegocio_IdCita",
                table: "PagoHistorial",
                columns: new[] { "IdNegocio", "IdCita" });

            migrationBuilder.CreateIndex(
                name: "IX_PagoHistorial_IdPago",
                table: "PagoHistorial",
                column: "IdPago");

            migrationBuilder.CreateIndex(
                name: "IX_PagoHistorial_UserId",
                table: "PagoHistorial",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pago_AnuladoPor",
                table: "Pago",
                column: "AnuladoPorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pago_AnuladoPor",
                table: "Pago");

            migrationBuilder.DropTable(
                name: "PagoHistorial");

            migrationBuilder.DropIndex(
                name: "IX_Pago_AnuladoPorUserId",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "AnuladoPorUserId",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "FechaAnulacion",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "FechaUltimaDevolucion",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "MontoDevuelto",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "ReferenciaAnulacion",
                table: "Pago");

            migrationBuilder.Sql("""
                DELETE FROM [dbo].[EstadoPago]
                WHERE [Nombre] IN (N'Parcialmente devuelto', N'Devuelto')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [dbo].[Pago]
                      WHERE [Pago].[IdEstadoPago] = [EstadoPago].[IdEstadoPago]
                  );
                """);
        }
    }
}
