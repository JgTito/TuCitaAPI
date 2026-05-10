using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuCita.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CanalNotificacion",
                columns: table => new
                {
                    IdCanalNotificacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanalNotificacion", x => x.IdCanalNotificacion);
                });

            migrationBuilder.CreateTable(
                name: "EstadoCita",
                columns: table => new
                {
                    IdEstadoCita = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EsEstadoFinal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoCita", x => x.IdEstadoCita);
                });

            migrationBuilder.CreateTable(
                name: "EstadoNotificacion",
                columns: table => new
                {
                    IdEstadoNotificacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoNotificacion", x => x.IdEstadoNotificacion);
                });

            migrationBuilder.CreateTable(
                name: "RolNegocio",
                columns: table => new
                {
                    IdRolNegocio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolNegocio", x => x.IdRolNegocio);
                });

            migrationBuilder.CreateTable(
                name: "Rubro",
                columns: table => new
                {
                    IdRubro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rubro", x => x.IdRubro);
                });

            migrationBuilder.CreateTable(
                name: "TipoCampo",
                columns: table => new
                {
                    IdTipoCampo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoCampo", x => x.IdTipoCampo);
                });

            migrationBuilder.CreateTable(
                name: "TipoNotificacion",
                columns: table => new
                {
                    IdTipoNotificacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoNotificacion", x => x.IdTipoNotificacion);
                });

            migrationBuilder.CreateTable(
                name: "TipoPrestador",
                columns: table => new
                {
                    IdTipoPrestador = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoPrestador", x => x.IdTipoPrestador);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Negocio",
                columns: table => new
                {
                    IdNegocio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdRubro = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("UQ_Negocio_IdNegocio", x => x.IdNegocio);
                    table.ForeignKey(
                        name: "FK_Negocio_Rubro",
                        column: x => x.IdRubro,
                        principalTable: "Rubro",
                        principalColumn: "IdRubro",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampoReserva",
                columns: table => new
                {
                    IdCampoReserva = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdTipoCampo = table.Column<int>(type: "int", nullable: false),
                    NombreInterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Etiqueta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Placeholder = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TextoAyuda = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Obligatorio = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampoReserva", x => x.IdCampoReserva);
                    table.UniqueConstraint("UQ_CampoReserva_Negocio_IdCampoReserva", x => new { x.IdNegocio, x.IdCampoReserva });
                    table.ForeignKey(
                        name: "FK_CampoReserva_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampoReserva_TipoCampo",
                        column: x => x.IdTipoCampo,
                        principalTable: "TipoCampo",
                        principalColumn: "IdTipoCampo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoriaServicio",
                columns: table => new
                {
                    IdCategoriaServicio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriaServicio", x => x.IdCategoriaServicio);
                    table.UniqueConstraint("UQ_CategoriaServicio_Negocio_IdCategoriaServicio", x => new { x.IdNegocio, x.IdCategoriaServicio });
                    table.ForeignKey(
                        name: "FK_CategoriaServicio_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cliente",
                columns: table => new
                {
                    IdCliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Rut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cliente", x => x.IdCliente);
                    table.UniqueConstraint("UQ_Cliente_Negocio_IdCliente", x => new { x.IdNegocio, x.IdCliente });
                    table.ForeignKey(
                        name: "FK_Cliente_AspNetUsers",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Cliente_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HorarioNegocio",
                columns: table => new
                {
                    IdHorarioNegocio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<byte>(type: "tinyint", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorarioNegocio", x => x.IdHorarioNegocio);
                    table.CheckConstraint("CK_HorarioNegocio_DiaSemana", "[DiaSemana] BETWEEN 1 AND 7");
                    table.CheckConstraint("CK_HorarioNegocio_Horas", "[HoraFin] > [HoraInicio]");
                    table.ForeignKey(
                        name: "FK_HorarioNegocio_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvitacionNegocio",
                columns: table => new
                {
                    IdInvitacionNegocio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdRolNegocio = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pendiente"),
                    InvitadoPorUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AceptadoPorUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CanceladoPorUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaAceptacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCancelacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaUltimoReenvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Mensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitacionNegocio", x => x.IdInvitacionNegocio);
                    table.CheckConstraint("CK_InvitacionNegocio_Estado", "[Estado] IN ('Pendiente', 'Aceptada', 'Expirada', 'Cancelada', 'Reenviada')");
                    table.ForeignKey(
                        name: "FK_InvitacionNegocio_AceptadoPor",
                        column: x => x.AceptadoPorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvitacionNegocio_CanceladoPor",
                        column: x => x.CanceladoPorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvitacionNegocio_InvitadoPor",
                        column: x => x.InvitadoPorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvitacionNegocio_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvitacionNegocio_RolNegocio",
                        column: x => x.IdRolNegocio,
                        principalTable: "RolNegocio",
                        principalColumn: "IdRolNegocio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NegocioUsuario",
                columns: table => new
                {
                    IdNegocioUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IdRolNegocio = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NegocioUsuario", x => x.IdNegocioUsuario);
                    table.ForeignKey(
                        name: "FK_NegocioUsuario_AspNetUsers",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NegocioUsuario_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NegocioUsuario_RolNegocio",
                        column: x => x.IdRolNegocio,
                        principalTable: "RolNegocio",
                        principalColumn: "IdRolNegocio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prestador",
                columns: table => new
                {
                    IdPrestador = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdTipoPrestador = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Capacidad = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestador", x => x.IdPrestador);
                    table.UniqueConstraint("UQ_Prestador_Negocio_IdPrestador", x => new { x.IdNegocio, x.IdPrestador });
                    table.CheckConstraint("CK_Prestador_Capacidad", "[Capacidad] > 0");
                    table.ForeignKey(
                        name: "FK_Prestador_AspNetUsers",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Prestador_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prestador_TipoPrestador",
                        column: x => x.IdTipoPrestador,
                        principalTable: "TipoPrestador",
                        principalColumn: "IdTipoPrestador",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReglaReserva",
                columns: table => new
                {
                    IdReglaReserva = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    MinHorasAnticipacion = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    MaxDiasAdelanto = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    PermiteCancelacionCliente = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    HorasLimiteCancelacion = table.Column<int>(type: "int", nullable: false, defaultValue: 6),
                    RequiereConfirmacionManual = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PermiteSobreturnos = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MaxCitasActivasPorCliente = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglaReserva", x => x.IdReglaReserva);
                    table.CheckConstraint("CK_ReglaReserva_HorasCancelacion", "[HorasLimiteCancelacion] >= 0");
                    table.CheckConstraint("CK_ReglaReserva_MaxCitas", "[MaxCitasActivasPorCliente] > 0");
                    table.CheckConstraint("CK_ReglaReserva_MaxDias", "[MaxDiasAdelanto] > 0");
                    table.CheckConstraint("CK_ReglaReserva_MinHoras", "[MinHorasAnticipacion] >= 0");
                    table.ForeignKey(
                        name: "FK_ReglaReserva_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampoReservaOpcion",
                columns: table => new
                {
                    IdCampoReservaOpcion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdCampoReserva = table.Column<int>(type: "int", nullable: false),
                    Etiqueta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampoReservaOpcion", x => x.IdCampoReservaOpcion);
                    table.ForeignKey(
                        name: "FK_CampoReservaOpcion_CampoReserva",
                        columns: x => new { x.IdNegocio, x.IdCampoReserva },
                        principalTable: "CampoReserva",
                        principalColumns: new[] { "IdNegocio", "IdCampoReserva" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Servicio",
                columns: table => new
                {
                    IdServicio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdCategoriaServicio = table.Column<int>(type: "int", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    RequiereProfesional = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RequierePagoAnticipado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TiempoPreparacionMinutos = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicio", x => x.IdServicio);
                    table.UniqueConstraint("UQ_Servicio_Negocio_IdServicio", x => new { x.IdNegocio, x.IdServicio });
                    table.CheckConstraint("CK_Servicio_Duracion", "[DuracionMinutos] > 0");
                    table.CheckConstraint("CK_Servicio_Precio", "[Precio] >= 0");
                    table.CheckConstraint("CK_Servicio_TiempoPreparacion", "[TiempoPreparacionMinutos] >= 0");
                    table.ForeignKey(
                        name: "FK_Servicio_CategoriaServicio",
                        columns: x => new { x.IdNegocio, x.IdCategoriaServicio },
                        principalTable: "CategoriaServicio",
                        principalColumns: new[] { "IdNegocio", "IdCategoriaServicio" });
                    table.ForeignKey(
                        name: "FK_Servicio_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BloqueoHorario",
                columns: table => new
                {
                    IdBloqueoHorario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdPrestador = table.Column<int>(type: "int", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueoHorario", x => x.IdBloqueoHorario);
                    table.CheckConstraint("CK_BloqueoHorario_Fechas", "[FechaFin] > [FechaInicio]");
                    table.ForeignKey(
                        name: "FK_BloqueoHorario_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BloqueoHorario_Prestador",
                        columns: x => new { x.IdNegocio, x.IdPrestador },
                        principalTable: "Prestador",
                        principalColumns: new[] { "IdNegocio", "IdPrestador" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HorarioPrestador",
                columns: table => new
                {
                    IdHorarioPrestador = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdPrestador = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<byte>(type: "tinyint", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorarioPrestador", x => x.IdHorarioPrestador);
                    table.CheckConstraint("CK_HorarioPrestador_DiaSemana", "[DiaSemana] BETWEEN 1 AND 7");
                    table.CheckConstraint("CK_HorarioPrestador_Horas", "[HoraFin] > [HoraInicio]");
                    table.ForeignKey(
                        name: "FK_HorarioPrestador_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HorarioPrestador_Prestador",
                        columns: x => new { x.IdNegocio, x.IdPrestador },
                        principalTable: "Prestador",
                        principalColumns: new[] { "IdNegocio", "IdPrestador" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cita",
                columns: table => new
                {
                    IdCita = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    IdServicio = table.Column<int>(type: "int", nullable: false),
                    IdPrestador = table.Column<int>(type: "int", nullable: true),
                    IdEstadoCita = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComentarioCliente = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NotaInterna = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PrecioEstimado = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cita", x => x.IdCita);
                    table.UniqueConstraint("UQ_Cita_Negocio_IdCita", x => new { x.IdNegocio, x.IdCita });
                    table.CheckConstraint("CK_Cita_Fechas", "[FechaFin] > [FechaInicio]");
                    table.CheckConstraint("CK_Cita_Precio", "[PrecioEstimado] >= 0");
                    table.ForeignKey(
                        name: "FK_Cita_Cliente",
                        columns: x => new { x.IdNegocio, x.IdCliente },
                        principalTable: "Cliente",
                        principalColumns: new[] { "IdNegocio", "IdCliente" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cita_EstadoCita",
                        column: x => x.IdEstadoCita,
                        principalTable: "EstadoCita",
                        principalColumn: "IdEstadoCita",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cita_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cita_Prestador",
                        columns: x => new { x.IdNegocio, x.IdPrestador },
                        principalTable: "Prestador",
                        principalColumns: new[] { "IdNegocio", "IdPrestador" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cita_Servicio",
                        columns: x => new { x.IdNegocio, x.IdServicio },
                        principalTable: "Servicio",
                        principalColumns: new[] { "IdNegocio", "IdServicio" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrestadorServicio",
                columns: table => new
                {
                    IdPrestadorServicio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdPrestador = table.Column<int>(type: "int", nullable: false),
                    IdServicio = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrestadorServicio", x => x.IdPrestadorServicio);
                    table.ForeignKey(
                        name: "FK_PrestadorServicio_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrestadorServicio_Prestador",
                        columns: x => new { x.IdNegocio, x.IdPrestador },
                        principalTable: "Prestador",
                        principalColumns: new[] { "IdNegocio", "IdPrestador" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrestadorServicio_Servicio",
                        columns: x => new { x.IdNegocio, x.IdServicio },
                        principalTable: "Servicio",
                        principalColumns: new[] { "IdNegocio", "IdServicio" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CitaCampoValor",
                columns: table => new
                {
                    IdCitaCampoValor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdCita = table.Column<int>(type: "int", nullable: false),
                    IdCampoReserva = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitaCampoValor", x => x.IdCitaCampoValor);
                    table.ForeignKey(
                        name: "FK_CitaCampoValor_CampoReserva",
                        columns: x => new { x.IdNegocio, x.IdCampoReserva },
                        principalTable: "CampoReserva",
                        principalColumns: new[] { "IdNegocio", "IdCampoReserva" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CitaCampoValor_Cita",
                        columns: x => new { x.IdNegocio, x.IdCita },
                        principalTable: "Cita",
                        principalColumns: new[] { "IdNegocio", "IdCita" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CitaHistorial",
                columns: table => new
                {
                    IdCitaHistorial = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdCita = table.Column<int>(type: "int", nullable: false),
                    IdEstadoAnterior = table.Column<int>(type: "int", nullable: true),
                    IdEstadoNuevo = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitaHistorial", x => x.IdCitaHistorial);
                    table.ForeignKey(
                        name: "FK_CitaHistorial_AspNetUsers",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CitaHistorial_Cita",
                        columns: x => new { x.IdNegocio, x.IdCita },
                        principalTable: "Cita",
                        principalColumns: new[] { "IdNegocio", "IdCita" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CitaHistorial_EstadoAnterior",
                        column: x => x.IdEstadoAnterior,
                        principalTable: "EstadoCita",
                        principalColumn: "IdEstadoCita",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CitaHistorial_EstadoNuevo",
                        column: x => x.IdEstadoNuevo,
                        principalTable: "EstadoCita",
                        principalColumn: "IdEstadoCita",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notificacion",
                columns: table => new
                {
                    IdNotificacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdNegocio = table.Column<int>(type: "int", nullable: false),
                    IdCita = table.Column<int>(type: "int", nullable: true),
                    IdTipoNotificacion = table.Column<int>(type: "int", nullable: false),
                    IdCanalNotificacion = table.Column<int>(type: "int", nullable: false),
                    IdEstadoNotificacion = table.Column<int>(type: "int", nullable: false),
                    Destinatario = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Asunto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaProgramada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacion", x => x.IdNotificacion);
                    table.ForeignKey(
                        name: "FK_Notificacion_CanalNotificacion",
                        column: x => x.IdCanalNotificacion,
                        principalTable: "CanalNotificacion",
                        principalColumn: "IdCanalNotificacion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notificacion_Cita",
                        columns: x => new { x.IdNegocio, x.IdCita },
                        principalTable: "Cita",
                        principalColumns: new[] { "IdNegocio", "IdCita" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notificacion_EstadoNotificacion",
                        column: x => x.IdEstadoNotificacion,
                        principalTable: "EstadoNotificacion",
                        principalColumn: "IdEstadoNotificacion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notificacion_Negocio",
                        column: x => x.IdNegocio,
                        principalTable: "Negocio",
                        principalColumn: "IdNegocio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notificacion_TipoNotificacion",
                        column: x => x.IdTipoNotificacion,
                        principalTable: "TipoNotificacion",
                        principalColumn: "IdTipoNotificacion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueoHorario_IdNegocio_Fechas",
                table: "BloqueoHorario",
                columns: new[] { "IdNegocio", "FechaInicio", "FechaFin" });

            migrationBuilder.CreateIndex(
                name: "IX_BloqueoHorario_IdNegocio_IdPrestador",
                table: "BloqueoHorario",
                columns: new[] { "IdNegocio", "IdPrestador" });

            migrationBuilder.CreateIndex(
                name: "IX_BloqueoHorario_IdPrestador_Fechas",
                table: "BloqueoHorario",
                columns: new[] { "IdPrestador", "FechaInicio", "FechaFin" });

            migrationBuilder.CreateIndex(
                name: "IX_CampoReserva_IdNegocio",
                table: "CampoReserva",
                column: "IdNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_CampoReserva_IdTipoCampo",
                table: "CampoReserva",
                column: "IdTipoCampo");

            migrationBuilder.CreateIndex(
                name: "UQ_CampoReserva_Negocio_NombreInterno",
                table: "CampoReserva",
                columns: new[] { "IdNegocio", "NombreInterno" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_CampoReservaOpcion",
                table: "CampoReservaOpcion",
                columns: new[] { "IdNegocio", "IdCampoReserva", "Valor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_CanalNotificacion_Nombre",
                table: "CanalNotificacion",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoriaServicio_IdNegocio",
                table: "CategoriaServicio",
                column: "IdNegocio");

            migrationBuilder.CreateIndex(
                name: "UQ_CategoriaServicio_Negocio_Nombre",
                table: "CategoriaServicio",
                columns: new[] { "IdNegocio", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cita_IdCliente",
                table: "Cita",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Cita_IdEstadoCita",
                table: "Cita",
                column: "IdEstadoCita");

            migrationBuilder.CreateIndex(
                name: "IX_Cita_IdNegocio_FechaInicio",
                table: "Cita",
                columns: new[] { "IdNegocio", "FechaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Cita_IdNegocio_IdCliente",
                table: "Cita",
                columns: new[] { "IdNegocio", "IdCliente" });

            migrationBuilder.CreateIndex(
                name: "IX_Cita_IdNegocio_IdPrestador",
                table: "Cita",
                columns: new[] { "IdNegocio", "IdPrestador" });

            migrationBuilder.CreateIndex(
                name: "IX_Cita_IdNegocio_IdServicio",
                table: "Cita",
                columns: new[] { "IdNegocio", "IdServicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Cita_IdPrestador_FechaInicio",
                table: "Cita",
                columns: new[] { "IdPrestador", "FechaInicio" });

            migrationBuilder.CreateIndex(
                name: "UQ_Cita_Negocio_Codigo",
                table: "Cita",
                columns: new[] { "IdNegocio", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CitaCampoValor_IdNegocio_IdCampoReserva",
                table: "CitaCampoValor",
                columns: new[] { "IdNegocio", "IdCampoReserva" });

            migrationBuilder.CreateIndex(
                name: "UQ_CitaCampoValor",
                table: "CitaCampoValor",
                columns: new[] { "IdNegocio", "IdCita", "IdCampoReserva" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CitaHistorial_IdCita",
                table: "CitaHistorial",
                column: "IdCita");

            migrationBuilder.CreateIndex(
                name: "IX_CitaHistorial_IdEstadoAnterior",
                table: "CitaHistorial",
                column: "IdEstadoAnterior");

            migrationBuilder.CreateIndex(
                name: "IX_CitaHistorial_IdEstadoNuevo",
                table: "CitaHistorial",
                column: "IdEstadoNuevo");

            migrationBuilder.CreateIndex(
                name: "IX_CitaHistorial_IdNegocio_IdCita",
                table: "CitaHistorial",
                columns: new[] { "IdNegocio", "IdCita" });

            migrationBuilder.CreateIndex(
                name: "IX_CitaHistorial_UserId",
                table: "CitaHistorial",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_IdNegocio",
                table: "Cliente",
                column: "IdNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_UserId",
                table: "Cliente",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_EstadoCita_Nombre",
                table: "EstadoCita",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_EstadoNotificacion_Nombre",
                table: "EstadoNotificacion",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HorarioNegocio_IdNegocio_DiaSemana",
                table: "HorarioNegocio",
                columns: new[] { "IdNegocio", "DiaSemana" });

            migrationBuilder.CreateIndex(
                name: "IX_HorarioPrestador_IdNegocio_IdPrestador",
                table: "HorarioPrestador",
                columns: new[] { "IdNegocio", "IdPrestador" });

            migrationBuilder.CreateIndex(
                name: "IX_HorarioPrestador_IdPrestador_DiaSemana",
                table: "HorarioPrestador",
                columns: new[] { "IdPrestador", "DiaSemana" });

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionNegocio_AceptadoPorUserId",
                table: "InvitacionNegocio",
                column: "AceptadoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionNegocio_CanceladoPorUserId",
                table: "InvitacionNegocio",
                column: "CanceladoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionNegocio_Estado_FechaExpiracion",
                table: "InvitacionNegocio",
                columns: new[] { "Estado", "FechaExpiracion" });

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionNegocio_IdNegocio",
                table: "InvitacionNegocio",
                column: "IdNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionNegocio_IdRolNegocio",
                table: "InvitacionNegocio",
                column: "IdRolNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionNegocio_InvitadoPorUserId",
                table: "InvitacionNegocio",
                column: "InvitadoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionNegocio_NormalizedEmail",
                table: "InvitacionNegocio",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UQ_InvitacionNegocio_Pendiente_Email_Negocio",
                table: "InvitacionNegocio",
                columns: new[] { "IdNegocio", "NormalizedEmail" },
                unique: true,
                filter: "[Estado] = 'Pendiente'");

            migrationBuilder.CreateIndex(
                name: "UQ_InvitacionNegocio_TokenHash",
                table: "InvitacionNegocio",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Negocio_IdRubro",
                table: "Negocio",
                column: "IdRubro");

            migrationBuilder.CreateIndex(
                name: "UQ_Negocio_Slug",
                table: "Negocio",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NegocioUsuario_IdNegocio",
                table: "NegocioUsuario",
                column: "IdNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_NegocioUsuario_IdRolNegocio",
                table: "NegocioUsuario",
                column: "IdRolNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_NegocioUsuario_UserId",
                table: "NegocioUsuario",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_NegocioUsuario",
                table: "NegocioUsuario",
                columns: new[] { "IdNegocio", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacion_IdCanalNotificacion",
                table: "Notificacion",
                column: "IdCanalNotificacion");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacion_IdCita",
                table: "Notificacion",
                column: "IdCita");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacion_IdNegocio",
                table: "Notificacion",
                column: "IdNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacion_IdNegocio_IdCita",
                table: "Notificacion",
                columns: new[] { "IdNegocio", "IdCita" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacion_IdTipoNotificacion",
                table: "Notificacion",
                column: "IdTipoNotificacion");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacion_Programada",
                table: "Notificacion",
                columns: new[] { "IdEstadoNotificacion", "FechaProgramada" });

            migrationBuilder.CreateIndex(
                name: "IX_Prestador_IdNegocio",
                table: "Prestador",
                column: "IdNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_Prestador_IdTipoPrestador",
                table: "Prestador",
                column: "IdTipoPrestador");

            migrationBuilder.CreateIndex(
                name: "IX_Prestador_UserId",
                table: "Prestador",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrestadorServicio_IdNegocio",
                table: "PrestadorServicio",
                column: "IdNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_PrestadorServicio_IdNegocio_IdServicio",
                table: "PrestadorServicio",
                columns: new[] { "IdNegocio", "IdServicio" });

            migrationBuilder.CreateIndex(
                name: "IX_PrestadorServicio_IdPrestador",
                table: "PrestadorServicio",
                column: "IdPrestador");

            migrationBuilder.CreateIndex(
                name: "IX_PrestadorServicio_IdServicio",
                table: "PrestadorServicio",
                column: "IdServicio");

            migrationBuilder.CreateIndex(
                name: "UQ_PrestadorServicio",
                table: "PrestadorServicio",
                columns: new[] { "IdNegocio", "IdPrestador", "IdServicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_ReglaReserva_Negocio",
                table: "ReglaReserva",
                column: "IdNegocio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_RolNegocio_Nombre",
                table: "RolNegocio",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Rubro_Nombre",
                table: "Rubro",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_IdCategoriaServicio",
                table: "Servicio",
                column: "IdCategoriaServicio");

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_IdNegocio",
                table: "Servicio",
                column: "IdNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_IdNegocio_IdCategoriaServicio",
                table: "Servicio",
                columns: new[] { "IdNegocio", "IdCategoriaServicio" });

            migrationBuilder.CreateIndex(
                name: "UQ_Servicio_Negocio_Nombre",
                table: "Servicio",
                columns: new[] { "IdNegocio", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TipoCampo_Nombre",
                table: "TipoCampo",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TipoNotificacion_Nombre",
                table: "TipoNotificacion",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TipoPrestador_Nombre",
                table: "TipoPrestador",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BloqueoHorario");

            migrationBuilder.DropTable(
                name: "CampoReservaOpcion");

            migrationBuilder.DropTable(
                name: "CitaCampoValor");

            migrationBuilder.DropTable(
                name: "CitaHistorial");

            migrationBuilder.DropTable(
                name: "HorarioNegocio");

            migrationBuilder.DropTable(
                name: "HorarioPrestador");

            migrationBuilder.DropTable(
                name: "InvitacionNegocio");

            migrationBuilder.DropTable(
                name: "NegocioUsuario");

            migrationBuilder.DropTable(
                name: "Notificacion");

            migrationBuilder.DropTable(
                name: "PrestadorServicio");

            migrationBuilder.DropTable(
                name: "ReglaReserva");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "CampoReserva");

            migrationBuilder.DropTable(
                name: "RolNegocio");

            migrationBuilder.DropTable(
                name: "CanalNotificacion");

            migrationBuilder.DropTable(
                name: "Cita");

            migrationBuilder.DropTable(
                name: "EstadoNotificacion");

            migrationBuilder.DropTable(
                name: "TipoNotificacion");

            migrationBuilder.DropTable(
                name: "TipoCampo");

            migrationBuilder.DropTable(
                name: "Cliente");

            migrationBuilder.DropTable(
                name: "EstadoCita");

            migrationBuilder.DropTable(
                name: "Prestador");

            migrationBuilder.DropTable(
                name: "Servicio");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "TipoPrestador");

            migrationBuilder.DropTable(
                name: "CategoriaServicio");

            migrationBuilder.DropTable(
                name: "Negocio");

            migrationBuilder.DropTable(
                name: "Rubro");
        }
    }
}
