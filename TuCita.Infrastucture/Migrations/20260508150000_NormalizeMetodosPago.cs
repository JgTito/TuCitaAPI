using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeMetodosPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetodoPago",
                columns: table => new
                {
                    IdMetodoPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EsManual = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    EsOnline = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetodoPago", x => x.IdMetodoPago);
                });

            migrationBuilder.InsertData(
                table: "MetodoPago",
                columns: new[] { "IdMetodoPago", "Nombre", "Descripcion", "EsManual", "EsOnline", "Activo" },
                values: new object[,]
                {
                    { 1, "Flow", "Pago online procesado por Flow", false, true, true },
                    { 2, "Efectivo", "Pago manual recibido en efectivo en el negocio", true, false, true },
                    { 3, "Transferencia bancaria", "Pago manual recibido por transferencia bancaria", true, false, true },
                    { 4, "Tarjeta debito", "Pago manual recibido con tarjeta de debito", true, false, true },
                    { 5, "Tarjeta credito", "Pago manual recibido con tarjeta de credito", true, false, true },
                    { 6, "POS", "Pago manual recibido mediante terminal POS o red de pago", true, false, true },
                    { 7, "Cheque", "Pago manual recibido con cheque", true, false, true },
                    { 8, "Otro", "Otro metodo de pago manual", true, false, true }
                });

            migrationBuilder.AddColumn<int>(
                name: "IdMetodoPago",
                table: "Pago",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Pago]
                SET [IdMetodoPago] =
                    CASE
                        WHEN [MetodoPago] = N'Efectivo' THEN 2
                        WHEN [MetodoPago] = N'Transferencia bancaria' THEN 3
                        WHEN [MetodoPago] = N'Tarjeta debito' THEN 4
                        WHEN [MetodoPago] = N'Tarjeta credito' THEN 5
                        WHEN [MetodoPago] = N'POS' THEN 6
                        WHEN [MetodoPago] = N'Cheque' THEN 7
                        WHEN [MetodoPago] = N'Otro' THEN 8
                        WHEN [Proveedor] = N'Manual' OR [EsManual] = 1 THEN 2
                        ELSE 1
                    END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "IdMetodoPago",
                table: "Pago",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Pago_MetodoPago",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Pago");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_IdMetodoPago",
                table: "Pago",
                column: "IdMetodoPago");

            migrationBuilder.CreateIndex(
                name: "UQ_MetodoPago_Nombre",
                table: "MetodoPago",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Pago_MetodoPago",
                table: "Pago",
                column: "IdMetodoPago",
                principalTable: "MetodoPago",
                principalColumn: "IdMetodoPago",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pago_MetodoPago",
                table: "Pago");

            migrationBuilder.DropIndex(
                name: "IX_Pago_IdMetodoPago",
                table: "Pago");

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "Pago",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Flow");

            migrationBuilder.Sql("""
                UPDATE p
                SET p.[MetodoPago] = m.[Nombre]
                FROM [Pago] p
                INNER JOIN [MetodoPago] m ON m.[IdMetodoPago] = p.[IdMetodoPago];
                """);

            migrationBuilder.DropColumn(
                name: "IdMetodoPago",
                table: "Pago");

            migrationBuilder.DropTable(
                name: "MetodoPago");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_MetodoPago",
                table: "Pago",
                column: "MetodoPago");
        }
    }
}
