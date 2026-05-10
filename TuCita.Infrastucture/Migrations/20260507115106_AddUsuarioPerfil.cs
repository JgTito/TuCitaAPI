using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsuarioPerfil",
                columns: table => new
                {
                    IdUsuarioPerfil = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NombreCompleto = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: true),
                    Rut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaNacimiento = table.Column<DateTime>(type: "date", nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TelefonoAlternativo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Comuna = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ciudad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Pais = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AceptaTerminos = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FechaAceptacionTerminos = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AceptaMarketing = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DebeCambiarPassword = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FechaUltimoCambioPassword = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaUltimoLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpUltimoLogin = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgentUltimoLogin = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioPerfil", x => x.IdUsuarioPerfil);
                    table.ForeignKey(
                        name: "FK_UsuarioPerfil_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_UsuarioPerfil_UserId",
                table: "UsuarioPerfil",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuarioPerfil");
        }
    }
}
