using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class AddFlowPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstadoPago",
                columns: table => new
                {
                    IdEstadoPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EsEstadoFinal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoPago", x => x.IdEstadoPago);
                });

            migrationBuilder.CreateTable(
                name: "Pago",
                columns: table => new
                {
                    IdPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdCita = table.Column<int>(type: "int", nullable: false),
                    IdEstadoPago = table.Column<int>(type: "int", nullable: false),
                    Proveedor = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CommerceOrder = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FlowOrder = table.Column<long>(type: "bigint", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Moneda = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "CLP"),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PayerEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    FlowStatus = table.Column<int>(type: "int", nullable: true),
                    FlowStatusNombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PaymentDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawCreateResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawStatusResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaUltimaConsulta = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pago", x => x.IdPago);
                    table.CheckConstraint("CK_Pago_Monto", "[Monto] >= 0");
                    table.ForeignKey(
                        name: "FK_Pago_Cita",
                        columns: x => new { x.IdNegocio, x.IdCita },
                        principalTable: "Cita",
                        principalColumns: new[] { "IdNegocio", "IdCita" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pago_EstadoPago",
                        column: x => x.IdEstadoPago,
                        principalTable: "EstadoPago",
                        principalColumn: "IdEstadoPago",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pago_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "EstadoPago",
                columns: new[] { "IdEstadoPago", "Nombre", "Descripcion", "EsEstadoFinal", "Activo" },
                values: new object[,]
                {
                    { 1, "Pendiente", "El pago fue creado y esta pendiente de confirmacion", false, true },
                    { 2, "Pagado", "El pago fue confirmado correctamente", true, true },
                    { 3, "Rechazado", "El pago fue rechazado por la pasarela", true, true },
                    { 4, "Anulado", "El pago fue anulado o expiro antes de completarse", true, true },
                    { 5, "Error", "Ocurrio un error al procesar el pago", true, true }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_EstadoPago_Nombre",
                table: "EstadoPago",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pago_FlowOrder",
                table: "Pago",
                column: "FlowOrder",
                filter: "[FlowOrder] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_IdCita",
                table: "Pago",
                column: "IdCita");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_IdEstadoPago",
                table: "Pago",
                column: "IdEstadoPago");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_IdNegocio",
                table: "Pago",
                column: "IdNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_IdNegocio_IdCita",
                table: "Pago",
                columns: new[] { "IdNegocio", "IdCita" });

            migrationBuilder.CreateIndex(
                name: "UQ_Pago_CommerceOrder",
                table: "Pago",
                column: "CommerceOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Pago_Token",
                table: "Pago",
                column: "Token",
                unique: true,
                filter: "[Token] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pago");

            migrationBuilder.DropTable(
                name: "EstadoPago");
        }
    }
}
