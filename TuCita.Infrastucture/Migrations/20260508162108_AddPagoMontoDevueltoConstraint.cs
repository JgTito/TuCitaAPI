using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class AddPagoMontoDevueltoConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Pago_MontoDevuelto",
                table: "Pago",
                sql: "[MontoDevuelto] >= 0 AND [MontoDevuelto] <= [Monto]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Pago_MontoDevuelto",
                table: "Pago");
        }
    }
}
