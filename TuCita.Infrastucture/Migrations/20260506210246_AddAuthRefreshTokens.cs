using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthRefreshToken",
                columns: table => new
                {
                    IdRefreshToken = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRevocacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReemplazadoPorTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthRefreshToken", x => x.IdRefreshToken);
                    table.ForeignKey(
                        name: "FK_AuthRefreshToken_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthRefreshToken_UserId",
                table: "AuthRefreshToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthRefreshToken_UserId_FechaExpiracion",
                table: "AuthRefreshToken",
                columns: new[] { "UserId", "FechaExpiracion" });

            migrationBuilder.CreateIndex(
                name: "UQ_AuthRefreshToken_TokenHash",
                table: "AuthRefreshToken",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthRefreshToken");
        }
    }
}
