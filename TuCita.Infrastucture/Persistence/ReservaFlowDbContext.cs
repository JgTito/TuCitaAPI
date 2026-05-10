using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TuCita.Infrastucture.Entities;

namespace TuCita.Infrastucture.Persistence;

public sealed class ReservaFlowDbContext(DbContextOptions<ReservaFlowDbContext> options)
    : IdentityDbContext<IdentityUser, IdentityRole, string>(options)
{
    public DbSet<Rubro> Rubros => Set<Rubro>();
    public DbSet<RolNegocio> RolesNegocio => Set<RolNegocio>();
    public DbSet<TipoPrestador> TiposPrestador => Set<TipoPrestador>();
    public DbSet<TipoCampo> TiposCampo => Set<TipoCampo>();
    public DbSet<EstadoCita> EstadosCita => Set<EstadoCita>();
    public DbSet<CanalNotificacion> CanalesNotificacion => Set<CanalNotificacion>();
    public DbSet<EstadoNotificacion> EstadosNotificacion => Set<EstadoNotificacion>();
    public DbSet<EstadoPago> EstadosPago => Set<EstadoPago>();
    public DbSet<MetodoPago> MetodosPago => Set<MetodoPago>();
    public DbSet<TipoNotificacion> TiposNotificacion => Set<TipoNotificacion>();
    public DbSet<Pais> Paises => Set<Pais>();
    public DbSet<Ciudad> Ciudades => Set<Ciudad>();
    public DbSet<Comuna> Comunas => Set<Comuna>();
    public DbSet<Negocio> Negocios => Set<Negocio>();
    public DbSet<NegocioUsuario> NegocioUsuarios => Set<NegocioUsuario>();
    public DbSet<CategoriaServicio> CategoriasServicio => Set<CategoriaServicio>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Prestador> Prestadores => Set<Prestador>();
    public DbSet<PrestadorServicio> PrestadorServicios => Set<PrestadorServicio>();
    public DbSet<HorarioNegocio> HorariosNegocio => Set<HorarioNegocio>();
    public DbSet<HorarioPrestador> HorariosPrestador => Set<HorarioPrestador>();
    public DbSet<BloqueoHorario> BloqueosHorario => Set<BloqueoHorario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<CampoReserva> CamposReserva => Set<CampoReserva>();
    public DbSet<CampoReservaOpcion> CampoReservaOpciones => Set<CampoReservaOpcion>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<CitaCampoValor> CitaCampoValores => Set<CitaCampoValor>();
    public DbSet<CitaHistorial> CitaHistoriales => Set<CitaHistorial>();
    public DbSet<ResenaNegocio> ResenasNegocio => Set<ResenaNegocio>();
    public DbSet<SolicitudResena> SolicitudesResena => Set<SolicitudResena>();
    public DbSet<ConfiguracionResenaNegocio> ConfiguracionesResenaNegocio => Set<ConfiguracionResenaNegocio>();
    public DbSet<ReglaReserva> ReglasReserva => Set<ReglaReserva>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<PagoHistorial> PagoHistoriales => Set<PagoHistorial>();
    public DbSet<AuditoriaEvento> AuditoriaEventos => Set<AuditoriaEvento>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<InvitacionNegocio> InvitacionesNegocio => Set<InvitacionNegocio>();
    public DbSet<AuthRefreshToken> AuthRefreshTokens => Set<AuthRefreshToken>();
    public DbSet<UsuarioPerfil> UsuariosPerfil => Set<UsuarioPerfil>();
    public DbSet<UsuarioContacto> UsuariosContacto => Set<UsuarioContacto>();
    public DbSet<UsuarioDireccion> UsuariosDireccion => Set<UsuarioDireccion>();
    public DbSet<UsuarioConsentimiento> UsuariosConsentimiento => Set<UsuarioConsentimiento>();
    public DbSet<UsuarioSeguridad> UsuariosSeguridad => Set<UsuarioSeguridad>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureIdentity(builder);
        ConfigureMasterTables(builder);
        ConfigureNegocios(builder);
        ConfigureServicios(builder);
        ConfigurePrestadores(builder);
        ConfigureHorarios(builder);
        ConfigureClientes(builder);
        ConfigureCamposPersonalizados(builder);
        ConfigureCitas(builder);
        ConfigureResenas(builder);
        ConfigureReglasReserva(builder);
        ConfigurePagos(builder);
        ConfigureAuditoria(builder);
        ConfigureNotificaciones(builder);
        ConfigureInvitaciones(builder);
        ConfigureAuthRefreshTokens(builder);
        ConfigureUbicaciones(builder);
        ConfigureUsuariosPerfil(builder);
    }

    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<IdentityUser>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(128);
            entity.Property(e => e.UserName).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
        });

        builder.Entity<IdentityRole>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.Property(e => e.RoleId).HasMaxLength(128);
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.Property(e => e.UserId).HasMaxLength(128);
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.ProviderKey).HasMaxLength(128);
            entity.Property(e => e.UserId).HasMaxLength(128);
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.RoleId).HasMaxLength(128);
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(128);
        });
    }

    private static void ConfigureMasterTables(ModelBuilder builder)
    {
        builder.Entity<Rubro>(entity =>
        {
            entity.ToTable("Rubro");
            entity.HasKey(e => e.IdRubro).HasName("PK_Rubro");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_Rubro_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        builder.Entity<RolNegocio>(entity =>
        {
            entity.ToTable("RolNegocio");
            entity.HasKey(e => e.IdRolNegocio).HasName("PK_RolNegocio");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_RolNegocio_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        builder.Entity<TipoPrestador>(entity =>
        {
            entity.ToTable("TipoPrestador");
            entity.HasKey(e => e.IdTipoPrestador).HasName("PK_TipoPrestador");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_TipoPrestador_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        builder.Entity<TipoCampo>(entity =>
        {
            entity.ToTable("TipoCampo");
            entity.HasKey(e => e.IdTipoCampo).HasName("PK_TipoCampo");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_TipoCampo_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        builder.Entity<EstadoCita>(entity =>
        {
            entity.ToTable("EstadoCita");
            entity.HasKey(e => e.IdEstadoCita).HasName("PK_EstadoCita");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_EstadoCita_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.EsEstadoFinal).HasDefaultValue(false);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        builder.Entity<CanalNotificacion>(entity =>
        {
            entity.ToTable("CanalNotificacion");
            entity.HasKey(e => e.IdCanalNotificacion).HasName("PK_CanalNotificacion");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_CanalNotificacion_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        builder.Entity<EstadoNotificacion>(entity =>
        {
            entity.ToTable("EstadoNotificacion");
            entity.HasKey(e => e.IdEstadoNotificacion).HasName("PK_EstadoNotificacion");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_EstadoNotificacion_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        builder.Entity<TipoNotificacion>(entity =>
        {
            entity.ToTable("TipoNotificacion");
            entity.HasKey(e => e.IdTipoNotificacion).HasName("PK_TipoNotificacion");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_TipoNotificacion_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        builder.Entity<EstadoPago>(entity =>
        {
            entity.ToTable("EstadoPago");
            entity.HasKey(e => e.IdEstadoPago).HasName("PK_EstadoPago");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_EstadoPago_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.EsEstadoFinal).HasDefaultValue(false);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        builder.Entity<MetodoPago>(entity =>
        {
            entity.ToTable("MetodoPago");
            entity.HasKey(e => e.IdMetodoPago).HasName("PK_MetodoPago");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_MetodoPago_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.EsManual).HasDefaultValue(false);
            entity.Property(e => e.EsOnline).HasDefaultValue(false);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });
    }

    private static void ConfigureNegocios(ModelBuilder builder)
    {
        builder.Entity<Negocio>(entity =>
        {
            entity.ToTable("Negocio");
            entity.HasKey(e => e.IdNegocio).HasName("PK_Negocio");
            entity.HasAlternateKey(e => e.IdNegocio).HasName("UQ_Negocio_IdNegocio");
            entity.HasIndex(e => e.IdRubro).HasDatabaseName("IX_Negocio_IdRubro");
            entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("UQ_Negocio_Slug");
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.Direccion).HasMaxLength(300);
            entity.Property(e => e.Telefono).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Rubro).WithMany(e => e.Negocios).HasForeignKey(e => e.IdRubro).HasConstraintName("FK_Negocio_Rubro");
        });

        builder.Entity<NegocioUsuario>(entity =>
        {
            entity.ToTable("NegocioUsuario");
            entity.HasKey(e => e.IdNegocioUsuario).HasName("PK_NegocioUsuario");
            entity.HasIndex(e => new { e.IdNegocio, e.UserId }).IsUnique().HasDatabaseName("UQ_NegocioUsuario");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_NegocioUsuario_UserId");
            entity.HasIndex(e => e.IdNegocio).HasDatabaseName("IX_NegocioUsuario_IdNegocio");
            entity.Property(e => e.UserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Negocio).WithMany(e => e.NegocioUsuarios).HasForeignKey(e => e.IdNegocio).HasConstraintName("FK_NegocioUsuario_Negocio");
            entity.HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.UserId).HasConstraintName("FK_NegocioUsuario_AspNetUsers");
            entity.HasOne(e => e.RolNegocio).WithMany(e => e.NegocioUsuarios).HasForeignKey(e => e.IdRolNegocio).HasConstraintName("FK_NegocioUsuario_RolNegocio");
        });
    }

    private static void ConfigureServicios(ModelBuilder builder)
    {
        builder.Entity<CategoriaServicio>(entity =>
        {
            entity.ToTable("CategoriaServicio");
            entity.HasKey(e => e.IdCategoriaServicio).HasName("PK_CategoriaServicio");
            entity.HasAlternateKey(e => new { e.IdNegocio, e.IdCategoriaServicio }).HasName("UQ_CategoriaServicio_Negocio_IdCategoriaServicio");
            entity.HasIndex(e => e.IdNegocio).HasDatabaseName("IX_CategoriaServicio_IdNegocio");
            entity.HasIndex(e => new { e.IdNegocio, e.Nombre }).IsUnique().HasDatabaseName("UQ_CategoriaServicio_Negocio_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasOne(e => e.Negocio).WithMany(e => e.CategoriasServicio).HasForeignKey(e => e.IdNegocio).HasConstraintName("FK_CategoriaServicio_Negocio");
        });

        builder.Entity<Servicio>(entity =>
        {
            entity.ToTable("Servicio", t =>
            {
                t.HasCheckConstraint("CK_Servicio_Duracion", "[DuracionMinutos] > 0");
                t.HasCheckConstraint("CK_Servicio_Precio", "[Precio] >= 0");
                t.HasCheckConstraint("CK_Servicio_TiempoPreparacion", "[TiempoPreparacionMinutos] >= 0");
            });
            entity.HasKey(e => e.IdServicio).HasName("PK_Servicio");
            entity.HasAlternateKey(e => new { e.IdNegocio, e.IdServicio }).HasName("UQ_Servicio_Negocio_IdServicio");
            entity.HasIndex(e => e.IdNegocio).HasDatabaseName("IX_Servicio_IdNegocio");
            entity.HasIndex(e => e.IdCategoriaServicio).HasDatabaseName("IX_Servicio_IdCategoriaServicio");
            entity.HasIndex(e => new { e.IdNegocio, e.Nombre }).IsUnique().HasDatabaseName("UQ_Servicio_Negocio_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Precio).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.RequiereProfesional).HasDefaultValue(true);
            entity.Property(e => e.RequierePagoAnticipado).HasDefaultValue(false);
            entity.Property(e => e.TiempoPreparacionMinutos).HasDefaultValue(0);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Negocio).WithMany(e => e.Servicios).HasForeignKey(e => e.IdNegocio).HasConstraintName("FK_Servicio_Negocio");
            entity.HasOne(e => e.CategoriaServicio).WithMany(e => e.Servicios).HasForeignKey(e => new { e.IdNegocio, e.IdCategoriaServicio }).HasPrincipalKey(e => new { e.IdNegocio, e.IdCategoriaServicio }).HasConstraintName("FK_Servicio_CategoriaServicio");
        });
    }

    private static void ConfigurePrestadores(ModelBuilder builder)
    {
        builder.Entity<Prestador>(entity =>
        {
            entity.ToTable("Prestador", t => t.HasCheckConstraint("CK_Prestador_Capacidad", "[Capacidad] > 0"));
            entity.HasKey(e => e.IdPrestador).HasName("PK_Prestador");
            entity.HasAlternateKey(e => new { e.IdNegocio, e.IdPrestador }).HasName("UQ_Prestador_Negocio_IdPrestador");
            entity.HasIndex(e => e.IdNegocio).HasDatabaseName("IX_Prestador_IdNegocio");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_Prestador_UserId");
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Telefono).HasMaxLength(30);
            entity.Property(e => e.Capacidad).HasDefaultValue(1);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Negocio).WithMany(e => e.Prestadores).HasForeignKey(e => e.IdNegocio).HasConstraintName("FK_Prestador_Negocio");
            entity.HasOne(e => e.TipoPrestador).WithMany(e => e.Prestadores).HasForeignKey(e => e.IdTipoPrestador).HasConstraintName("FK_Prestador_TipoPrestador");
            entity.HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.UserId).HasConstraintName("FK_Prestador_AspNetUsers");
        });

        builder.Entity<PrestadorServicio>(entity =>
        {
            entity.ToTable("PrestadorServicio");
            entity.HasKey(e => e.IdPrestadorServicio).HasName("PK_PrestadorServicio");
            entity.HasIndex(e => new { e.IdNegocio, e.IdPrestador, e.IdServicio }).IsUnique().HasDatabaseName("UQ_PrestadorServicio");
            entity.HasIndex(e => e.IdNegocio).HasDatabaseName("IX_PrestadorServicio_IdNegocio");
            entity.HasIndex(e => e.IdPrestador).HasDatabaseName("IX_PrestadorServicio_IdPrestador");
            entity.HasIndex(e => e.IdServicio).HasDatabaseName("IX_PrestadorServicio_IdServicio");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasOne(e => e.Negocio).WithMany(e => e.PrestadorServicios).HasForeignKey(e => e.IdNegocio).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PrestadorServicio_Negocio");
            entity.HasOne(e => e.Prestador).WithMany(e => e.PrestadorServicios).HasForeignKey(e => new { e.IdNegocio, e.IdPrestador }).HasPrincipalKey(e => new { e.IdNegocio, e.IdPrestador }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PrestadorServicio_Prestador");
            entity.HasOne(e => e.Servicio).WithMany(e => e.PrestadorServicios).HasForeignKey(e => new { e.IdNegocio, e.IdServicio }).HasPrincipalKey(e => new { e.IdNegocio, e.IdServicio }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PrestadorServicio_Servicio");
        });
    }

    private static void ConfigureHorarios(ModelBuilder builder)
    {
        builder.Entity<HorarioNegocio>(entity =>
        {
            entity.ToTable("HorarioNegocio", t =>
            {
                t.HasCheckConstraint("CK_HorarioNegocio_DiaSemana", "[DiaSemana] BETWEEN 1 AND 7");
                t.HasCheckConstraint("CK_HorarioNegocio_Horas", "[HoraFin] > [HoraInicio]");
            });
            entity.HasKey(e => e.IdHorarioNegocio).HasName("PK_HorarioNegocio");
            entity.HasIndex(e => new { e.IdNegocio, e.DiaSemana }).HasDatabaseName("IX_HorarioNegocio_IdNegocio_DiaSemana");
            entity.Property(e => e.HoraInicio).HasColumnType("time");
            entity.Property(e => e.HoraFin).HasColumnType("time");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasOne(e => e.Negocio).WithMany(e => e.HorariosNegocio).HasForeignKey(e => e.IdNegocio).HasConstraintName("FK_HorarioNegocio_Negocio");
        });

        builder.Entity<HorarioPrestador>(entity =>
        {
            entity.ToTable("HorarioPrestador", t =>
            {
                t.HasCheckConstraint("CK_HorarioPrestador_DiaSemana", "[DiaSemana] BETWEEN 1 AND 7");
                t.HasCheckConstraint("CK_HorarioPrestador_Horas", "[HoraFin] > [HoraInicio]");
            });
            entity.HasKey(e => e.IdHorarioPrestador).HasName("PK_HorarioPrestador");
            entity.HasIndex(e => new { e.IdPrestador, e.DiaSemana }).HasDatabaseName("IX_HorarioPrestador_IdPrestador_DiaSemana");
            entity.Property(e => e.HoraInicio).HasColumnType("time");
            entity.Property(e => e.HoraFin).HasColumnType("time");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasOne(e => e.Negocio).WithMany(e => e.HorariosPrestador).HasForeignKey(e => e.IdNegocio).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_HorarioPrestador_Negocio");
            entity.HasOne(e => e.Prestador).WithMany(e => e.HorariosPrestador).HasForeignKey(e => new { e.IdNegocio, e.IdPrestador }).HasPrincipalKey(e => new { e.IdNegocio, e.IdPrestador }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_HorarioPrestador_Prestador");
        });

        builder.Entity<BloqueoHorario>(entity =>
        {
            entity.ToTable("BloqueoHorario", t => t.HasCheckConstraint("CK_BloqueoHorario_Fechas", "[FechaFin] > [FechaInicio]"));
            entity.HasKey(e => e.IdBloqueoHorario).HasName("PK_BloqueoHorario");
            entity.HasIndex(e => new { e.IdNegocio, e.FechaInicio, e.FechaFin }).HasDatabaseName("IX_BloqueoHorario_IdNegocio_Fechas");
            entity.HasIndex(e => new { e.IdPrestador, e.FechaInicio, e.FechaFin }).HasDatabaseName("IX_BloqueoHorario_IdPrestador_Fechas");
            entity.Property(e => e.Motivo).HasMaxLength(300);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Negocio).WithMany(e => e.BloqueosHorario).HasForeignKey(e => e.IdNegocio).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_BloqueoHorario_Negocio");
            entity.HasOne(e => e.Prestador).WithMany(e => e.BloqueosHorario).HasForeignKey(e => new { e.IdNegocio, e.IdPrestador }).HasPrincipalKey(e => new { e.IdNegocio, e.IdPrestador }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_BloqueoHorario_Prestador");
        });
    }

    private static void ConfigureClientes(ModelBuilder builder)
    {
        builder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Cliente");
            entity.HasKey(e => e.IdCliente).HasName("PK_Cliente");
            entity.HasAlternateKey(e => new { e.IdNegocio, e.IdCliente }).HasName("UQ_Cliente_Negocio_IdCliente");
            entity.HasIndex(e => e.IdNegocio).HasDatabaseName("IX_Cliente_IdNegocio");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_Cliente_UserId");
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Telefono).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Rut).HasMaxLength(20);
            entity.Property(e => e.Notas).HasMaxLength(1000);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Negocio).WithMany(e => e.Clientes).HasForeignKey(e => e.IdNegocio).HasConstraintName("FK_Cliente_Negocio");
            entity.HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.UserId).HasConstraintName("FK_Cliente_AspNetUsers");
        });
    }

    private static void ConfigureCamposPersonalizados(ModelBuilder builder)
    {
        builder.Entity<CampoReserva>(entity =>
        {
            entity.ToTable("CampoReserva");
            entity.HasKey(e => e.IdCampoReserva).HasName("PK_CampoReserva");
            entity.HasAlternateKey(e => new { e.IdNegocio, e.IdCampoReserva }).HasName("UQ_CampoReserva_Negocio_IdCampoReserva");
            entity.HasIndex(e => e.IdNegocio).HasDatabaseName("IX_CampoReserva_IdNegocio");
            entity.HasIndex(e => e.IdServicio).HasDatabaseName("IX_CampoReserva_IdServicio");
            entity.HasIndex(e => new { e.IdNegocio, e.NombreInterno })
                .IsUnique()
                .HasFilter("[IdServicio] IS NULL")
                .HasDatabaseName("UQ_CampoReserva_Negocio_NombreInterno_Global");
            entity.HasIndex(e => new { e.IdNegocio, e.IdServicio, e.NombreInterno })
                .IsUnique()
                .HasFilter("[IdServicio] IS NOT NULL")
                .HasDatabaseName("UQ_CampoReserva_Negocio_Servicio_NombreInterno");
            entity.Property(e => e.NombreInterno).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Etiqueta).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Placeholder).HasMaxLength(150);
            entity.Property(e => e.TextoAyuda).HasMaxLength(300);
            entity.Property(e => e.Obligatorio).HasDefaultValue(false);
            entity.Property(e => e.Orden).HasDefaultValue(0);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasOne(e => e.Negocio).WithMany(e => e.CamposReserva).HasForeignKey(e => e.IdNegocio).HasConstraintName("FK_CampoReserva_Negocio");
            entity.HasOne(e => e.Servicio).WithMany(e => e.CamposReserva).HasForeignKey(e => new { e.IdNegocio, e.IdServicio }).HasPrincipalKey(e => new { e.IdNegocio, e.IdServicio }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CampoReserva_Servicio");
            entity.HasOne(e => e.TipoCampo).WithMany(e => e.CamposReserva).HasForeignKey(e => e.IdTipoCampo).HasConstraintName("FK_CampoReserva_TipoCampo");
        });

        builder.Entity<CampoReservaOpcion>(entity =>
        {
            entity.ToTable("CampoReservaOpcion");
            entity.HasKey(e => e.IdCampoReservaOpcion).HasName("PK_CampoReservaOpcion");
            entity.HasIndex(e => new { e.IdNegocio, e.IdCampoReserva, e.Valor }).IsUnique().HasDatabaseName("UQ_CampoReservaOpcion");
            entity.Property(e => e.Etiqueta).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Valor).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Orden).HasDefaultValue(0);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasOne(e => e.CampoReserva).WithMany(e => e.Opciones).HasForeignKey(e => new { e.IdNegocio, e.IdCampoReserva }).HasPrincipalKey(e => new { e.IdNegocio, e.IdCampoReserva }).HasConstraintName("FK_CampoReservaOpcion_CampoReserva");
        });
    }

    private static void ConfigureCitas(ModelBuilder builder)
    {
        builder.Entity<Cita>(entity =>
        {
            entity.ToTable("Cita", t =>
            {
                t.HasCheckConstraint("CK_Cita_Fechas", "[FechaFin] > [FechaInicio]");
                t.HasCheckConstraint("CK_Cita_Precio", "[PrecioEstimado] >= 0");
            });
            entity.HasKey(e => e.IdCita).HasName("PK_Cita");
            entity.HasAlternateKey(e => new { e.IdNegocio, e.IdCita }).HasName("UQ_Cita_Negocio_IdCita");
            entity.HasIndex(e => new { e.IdNegocio, e.Codigo }).IsUnique().HasDatabaseName("UQ_Cita_Negocio_Codigo");
            entity.HasIndex(e => new { e.IdNegocio, e.FechaInicio }).HasDatabaseName("IX_Cita_IdNegocio_FechaInicio");
            entity.HasIndex(e => new { e.IdPrestador, e.FechaInicio }).HasDatabaseName("IX_Cita_IdPrestador_FechaInicio");
            entity.HasIndex(e => e.IdCliente).HasDatabaseName("IX_Cita_IdCliente");
            entity.HasIndex(e => e.IdEstadoCita).HasDatabaseName("IX_Cita_IdEstadoCita");
            entity.Property(e => e.Codigo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ComentarioCliente).HasMaxLength(1000);
            entity.Property(e => e.NotaInterna).HasMaxLength(1000);
            entity.Property(e => e.PrecioEstimado).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Negocio).WithMany(e => e.Citas).HasForeignKey(e => e.IdNegocio).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Cita_Negocio");
            entity.HasOne(e => e.Cliente).WithMany(e => e.Citas).HasForeignKey(e => new { e.IdNegocio, e.IdCliente }).HasPrincipalKey(e => new { e.IdNegocio, e.IdCliente }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Cita_Cliente");
            entity.HasOne(e => e.Servicio).WithMany(e => e.Citas).HasForeignKey(e => new { e.IdNegocio, e.IdServicio }).HasPrincipalKey(e => new { e.IdNegocio, e.IdServicio }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Cita_Servicio");
            entity.HasOne(e => e.Prestador).WithMany(e => e.Citas).HasForeignKey(e => new { e.IdNegocio, e.IdPrestador }).HasPrincipalKey(e => new { e.IdNegocio, e.IdPrestador }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Cita_Prestador");
            entity.HasOne(e => e.EstadoCita).WithMany(e => e.Citas).HasForeignKey(e => e.IdEstadoCita).HasConstraintName("FK_Cita_EstadoCita");
        });

        builder.Entity<CitaCampoValor>(entity =>
        {
            entity.ToTable("CitaCampoValor");
            entity.HasKey(e => e.IdCitaCampoValor).HasName("PK_CitaCampoValor");
            entity.HasIndex(e => new { e.IdNegocio, e.IdCita, e.IdCampoReserva }).IsUnique().HasDatabaseName("UQ_CitaCampoValor");
            entity.HasOne(e => e.Cita).WithMany(e => e.CamposValor).HasForeignKey(e => new { e.IdNegocio, e.IdCita }).HasPrincipalKey(e => new { e.IdNegocio, e.IdCita }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CitaCampoValor_Cita");
            entity.HasOne(e => e.CampoReserva).WithMany(e => e.Valores).HasForeignKey(e => new { e.IdNegocio, e.IdCampoReserva }).HasPrincipalKey(e => new { e.IdNegocio, e.IdCampoReserva }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CitaCampoValor_CampoReserva");
        });

        builder.Entity<CitaHistorial>(entity =>
        {
            entity.ToTable("CitaHistorial");
            entity.HasKey(e => e.IdCitaHistorial).HasName("PK_CitaHistorial");
            entity.HasIndex(e => e.IdCita).HasDatabaseName("IX_CitaHistorial_IdCita");
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.Observacion).HasMaxLength(1000);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Cita).WithMany(e => e.Historial).HasForeignKey(e => new { e.IdNegocio, e.IdCita }).HasPrincipalKey(e => new { e.IdNegocio, e.IdCita }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CitaHistorial_Cita");
            entity.HasOne(e => e.EstadoAnterior).WithMany(e => e.HistorialesEstadoAnterior).HasForeignKey(e => e.IdEstadoAnterior).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CitaHistorial_EstadoAnterior");
            entity.HasOne(e => e.EstadoNuevo).WithMany(e => e.HistorialesEstadoNuevo).HasForeignKey(e => e.IdEstadoNuevo).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CitaHistorial_EstadoNuevo");
            entity.HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.UserId).HasConstraintName("FK_CitaHistorial_AspNetUsers");
        });
    }

    private static void ConfigureResenas(ModelBuilder builder)
    {
        builder.Entity<ConfiguracionResenaNegocio>(entity =>
        {
            entity.ToTable("ConfiguracionResenaNegocio", t =>
            {
                t.HasCheckConstraint("CK_ConfiguracionResenaNegocio_DiasMaximos", "[DiasMaximosParaCalificar] BETWEEN 1 AND 365");
                t.HasCheckConstraint("CK_ConfiguracionResenaNegocio_PuntuacionAlerta", "[PuntuacionMaximaAlertaOperativa] BETWEEN 1 AND 5");
            });

            entity.HasKey(e => e.IdConfiguracionResenaNegocio).HasName("PK_ConfiguracionResenaNegocio");
            entity.HasIndex(e => e.IdNegocio).IsUnique().HasDatabaseName("UQ_ConfiguracionResenaNegocio_Negocio");
            entity.Property(e => e.ResenasActivas).HasDefaultValue(true);
            entity.Property(e => e.AutoaprobarResenas).HasDefaultValue(false);
            entity.Property(e => e.DiasMaximosParaCalificar).HasDefaultValue(15);
            entity.Property(e => e.PuntuacionMaximaAlertaOperativa).HasDefaultValue((byte)2);
            entity.Property(e => e.PermitirRespuestaNegocio).HasDefaultValue(true);
            entity.Property(e => e.MostrarResenasPublicas).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Negocio).WithMany(e => e.ConfiguracionesResena).HasForeignKey(e => e.IdNegocio).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ConfiguracionResenaNegocio_Negocio");
        });

        builder.Entity<ResenaNegocio>(entity =>
        {
            entity.ToTable("ResenaNegocio", t =>
            {
                t.HasCheckConstraint("CK_ResenaNegocio_Puntuacion", "[Puntuacion] BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_ResenaNegocio_Estado", "[Estado] IN ('Pendiente', 'Aprobada', 'Rechazada', 'Oculta')");
            });

            entity.HasKey(e => e.IdResenaNegocio).HasName("PK_ResenaNegocio");
            entity.HasIndex(e => new { e.IdNegocio, e.IdCita }).IsUnique().HasDatabaseName("UQ_ResenaNegocio_Negocio_Cita");
            entity.HasIndex(e => new { e.IdNegocio, e.Estado, e.FechaCreacion }).HasDatabaseName("IX_ResenaNegocio_Negocio_Estado_Fecha");
            entity.HasIndex(e => new { e.IdNegocio, e.EsVisiblePublicamente, e.FechaPublicacion }).HasDatabaseName("IX_ResenaNegocio_Publicas");
            entity.HasIndex(e => new { e.IdNegocio, e.EsAlertaOperativa, e.FechaAlertaOperativa }).HasDatabaseName("IX_ResenaNegocio_AlertaOperativa");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_ResenaNegocio_UserId");
            entity.HasIndex(e => e.IdServicio).HasDatabaseName("IX_ResenaNegocio_IdServicio");
            entity.HasIndex(e => e.IdPrestador).HasDatabaseName("IX_ResenaNegocio_IdPrestador");
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.Estado).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Comentario).HasMaxLength(1500);
            entity.Property(e => e.ModeradoPorUserId).HasMaxLength(128);
            entity.Property(e => e.MotivoModeracion).HasMaxLength(300);
            entity.Property(e => e.RespuestaNegocio).HasMaxLength(1000);
            entity.Property(e => e.RespondidoPorUserId).HasMaxLength(128);
            entity.Property(e => e.EsAlertaOperativa).HasDefaultValue(false);
            entity.Property(e => e.MotivoAlertaOperativa).HasMaxLength(300);
            entity.Property(e => e.ClienteNombreSnapshot).HasMaxLength(150).IsRequired();
            entity.Property(e => e.ServicioNombreSnapshot).HasMaxLength(150).IsRequired();
            entity.Property(e => e.PrestadorNombreSnapshot).HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasOne(e => e.Negocio).WithMany(e => e.Resenas).HasForeignKey(e => e.IdNegocio).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ResenaNegocio_Negocio");
            entity.HasOne(e => e.Cita).WithMany(e => e.Resenas).HasForeignKey(e => new { e.IdNegocio, e.IdCita }).HasPrincipalKey(e => new { e.IdNegocio, e.IdCita }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ResenaNegocio_Cita");
            entity.HasOne(e => e.Cliente).WithMany(e => e.Resenas).HasForeignKey(e => new { e.IdNegocio, e.IdCliente }).HasPrincipalKey(e => new { e.IdNegocio, e.IdCliente }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ResenaNegocio_Cliente");
            entity.HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ResenaNegocio_AspNetUsers");
            entity.HasOne(e => e.Servicio).WithMany(e => e.Resenas).HasForeignKey(e => new { e.IdNegocio, e.IdServicio }).HasPrincipalKey(e => new { e.IdNegocio, e.IdServicio }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ResenaNegocio_Servicio");
            entity.HasOne(e => e.Prestador).WithMany(e => e.Resenas).HasForeignKey(e => new { e.IdNegocio, e.IdPrestador }).HasPrincipalKey(e => new { e.IdNegocio, e.IdPrestador }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ResenaNegocio_Prestador");
            entity.HasOne(e => e.ModeradoPor).WithMany().HasForeignKey(e => e.ModeradoPorUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ResenaNegocio_ModeradoPor");
            entity.HasOne(e => e.RespondidoPor).WithMany().HasForeignKey(e => e.RespondidoPorUserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_ResenaNegocio_RespondidoPor");
        });

        builder.Entity<SolicitudResena>(entity =>
        {
            entity.ToTable("SolicitudResena", t =>
            {
                t.HasCheckConstraint("CK_SolicitudResena_Estado", "[Estado] IN ('Pendiente', 'Usada', 'Expirada', 'Cancelada')");
            });

            entity.HasKey(e => e.IdSolicitudResena).HasName("PK_SolicitudResena");
            entity.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("UQ_SolicitudResena_TokenHash");
            entity.HasIndex(e => e.NormalizedEmail).HasDatabaseName("IX_SolicitudResena_NormalizedEmail");
            entity.HasIndex(e => new { e.Estado, e.FechaExpiracion }).HasDatabaseName("IX_SolicitudResena_Estado_FechaExpiracion");
            entity.HasIndex(e => new { e.IdNegocio, e.IdCita }).IsUnique().HasFilter("[Estado] = 'Pendiente'").HasDatabaseName("UQ_SolicitudResena_Pendiente_Negocio_Cita");
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.TokenHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(30).IsRequired();
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Negocio).WithMany(e => e.SolicitudesResena).HasForeignKey(e => e.IdNegocio).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SolicitudResena_Negocio");
            entity.HasOne(e => e.Cita).WithMany(e => e.SolicitudesResena).HasForeignKey(e => new { e.IdNegocio, e.IdCita }).HasPrincipalKey(e => new { e.IdNegocio, e.IdCita }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SolicitudResena_Cita");
            entity.HasOne(e => e.Cliente).WithMany(e => e.SolicitudesResena).HasForeignKey(e => new { e.IdNegocio, e.IdCliente }).HasPrincipalKey(e => new { e.IdNegocio, e.IdCliente }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SolicitudResena_Cliente");
        });
    }

    private static void ConfigureReglasReserva(ModelBuilder builder)
    {
        builder.Entity<ReglaReserva>(entity =>
        {
            entity.ToTable("ReglaReserva", t =>
            {
                t.HasCheckConstraint("CK_ReglaReserva_MinHoras", "[MinHorasAnticipacion] >= 0");
                t.HasCheckConstraint("CK_ReglaReserva_MaxDias", "[MaxDiasAdelanto] > 0");
                t.HasCheckConstraint("CK_ReglaReserva_HorasCancelacion", "[HorasLimiteCancelacion] >= 0");
                t.HasCheckConstraint("CK_ReglaReserva_MaxCitas", "[MaxCitasActivasPorCliente] > 0");
            });
            entity.HasKey(e => e.IdReglaReserva).HasName("PK_ReglaReserva");
            entity.HasIndex(e => e.IdNegocio).IsUnique().HasDatabaseName("UQ_ReglaReserva_Negocio");
            entity.Property(e => e.MinHorasAnticipacion).HasDefaultValue(2);
            entity.Property(e => e.MaxDiasAdelanto).HasDefaultValue(30);
            entity.Property(e => e.PermiteCancelacionCliente).HasDefaultValue(true);
            entity.Property(e => e.HorasLimiteCancelacion).HasDefaultValue(6);
            entity.Property(e => e.RequiereConfirmacionManual).HasDefaultValue(false);
            entity.Property(e => e.PermiteSobreturnos).HasDefaultValue(false);
            entity.Property(e => e.MaxCitasActivasPorCliente).HasDefaultValue(1);
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Negocio).WithMany(e => e.ReglasReserva).HasForeignKey(e => e.IdNegocio).HasConstraintName("FK_ReglaReserva_Negocio");
        });
    }

    private static void ConfigurePagos(ModelBuilder builder)
    {
        builder.Entity<Pago>(entity =>
        {
            entity.ToTable("Pago", table =>
            {
                table.HasCheckConstraint("CK_Pago_Monto", "[Monto] >= 0");
                table.HasCheckConstraint("CK_Pago_MontoDevuelto", "[MontoDevuelto] >= 0 AND [MontoDevuelto] <= [Monto]");
            });

            entity.HasKey(e => e.IdPago).HasName("PK_Pago");
            entity.HasIndex(e => e.IdNegocio).HasDatabaseName("IX_Pago_IdNegocio");
            entity.HasIndex(e => e.IdCita).HasDatabaseName("IX_Pago_IdCita");
            entity.HasIndex(e => e.IdEstadoPago).HasDatabaseName("IX_Pago_IdEstadoPago");
            entity.HasIndex(e => e.IdMetodoPago).HasDatabaseName("IX_Pago_IdMetodoPago");
            entity.HasIndex(e => e.RegistradoPorUserId).HasDatabaseName("IX_Pago_RegistradoPorUserId");
            entity.HasIndex(e => e.AnuladoPorUserId).HasDatabaseName("IX_Pago_AnuladoPorUserId");
            entity.HasIndex(e => e.EsManual).HasDatabaseName("IX_Pago_EsManual");
            entity.HasIndex(e => e.CommerceOrder).IsUnique().HasDatabaseName("UQ_Pago_CommerceOrder");
            entity.HasIndex(e => e.Token).IsUnique().HasFilter("[Token] IS NOT NULL").HasDatabaseName("UQ_Pago_Token");
            entity.HasIndex(e => e.FlowOrder).HasFilter("[FlowOrder] IS NOT NULL").HasDatabaseName("IX_Pago_FlowOrder");

            entity.Property(e => e.RegistradoPorUserId).HasMaxLength(128);
            entity.Property(e => e.Proveedor).HasMaxLength(40).IsRequired();
            entity.Property(e => e.EsManual).HasDefaultValue(false);
            entity.Property(e => e.CommerceOrder).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Token).HasMaxLength(150);
            entity.Property(e => e.CheckoutUrl).HasMaxLength(1000);
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Moneda).HasMaxLength(10).HasDefaultValue("CLP").IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(250);
            entity.Property(e => e.PayerEmail).HasMaxLength(150);
            entity.Property(e => e.FlowStatusNombre).HasMaxLength(80);
            entity.Property(e => e.ReferenciaManual).HasMaxLength(100);
            entity.Property(e => e.ObservacionManual).HasMaxLength(500);
            entity.Property(e => e.MontoDevuelto).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.MotivoAnulacion).HasMaxLength(500);
            entity.Property(e => e.ReferenciaAnulacion).HasMaxLength(100);
            entity.Property(e => e.AnuladoPorUserId).HasMaxLength(128);
            entity.Property(e => e.Error).HasMaxLength(1000);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(e => e.Negocio)
                .WithMany(e => e.Pagos)
                .HasForeignKey(e => e.IdNegocio)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Pago_Negocio");

            entity.HasOne(e => e.Cita)
                .WithMany(e => e.Pagos)
                .HasForeignKey(e => new { e.IdNegocio, e.IdCita })
                .HasPrincipalKey(e => new { e.IdNegocio, e.IdCita })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Pago_Cita");

            entity.HasOne(e => e.EstadoPago)
                .WithMany(e => e.Pagos)
                .HasForeignKey(e => e.IdEstadoPago)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Pago_EstadoPago");

            entity.HasOne(e => e.MetodoPago)
                .WithMany(e => e.Pagos)
                .HasForeignKey(e => e.IdMetodoPago)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Pago_MetodoPago");

            entity.HasOne(e => e.RegistradoPor)
                .WithMany()
                .HasForeignKey(e => e.RegistradoPorUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Pago_RegistradoPor");

            entity.HasOne(e => e.AnuladoPor)
                .WithMany()
                .HasForeignKey(e => e.AnuladoPorUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Pago_AnuladoPor");
        });

        builder.Entity<PagoHistorial>(entity =>
        {
            entity.ToTable("PagoHistorial", table =>
            {
                table.HasCheckConstraint("CK_PagoHistorial_Monto", "[Monto] IS NULL OR [Monto] >= 0");
            });

            entity.HasKey(e => e.IdPagoHistorial).HasName("PK_PagoHistorial");
            entity.HasIndex(e => e.IdPago).HasDatabaseName("IX_PagoHistorial_IdPago");
            entity.HasIndex(e => e.IdNegocio).HasDatabaseName("IX_PagoHistorial_IdNegocio");
            entity.HasIndex(e => new { e.IdNegocio, e.IdCita }).HasDatabaseName("IX_PagoHistorial_IdNegocio_IdCita");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_PagoHistorial_UserId");
            entity.HasIndex(e => e.FechaCreacion).HasDatabaseName("IX_PagoHistorial_FechaCreacion");
            entity.Property(e => e.TipoEvento).HasMaxLength(40).IsRequired();
            entity.Property(e => e.EstadoAnterior).HasMaxLength(80);
            entity.Property(e => e.EstadoNuevo).HasMaxLength(80);
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Motivo).HasMaxLength(500);
            entity.Property(e => e.Referencia).HasMaxLength(100);
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(e => e.Pago)
                .WithMany(e => e.Historial)
                .HasForeignKey(e => e.IdPago)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PagoHistorial_Pago");

            entity.HasOne(e => e.Negocio)
                .WithMany()
                .HasForeignKey(e => e.IdNegocio)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PagoHistorial_Negocio");

            entity.HasOne(e => e.Cita)
                .WithMany()
                .HasForeignKey(e => new { e.IdNegocio, e.IdCita })
                .HasPrincipalKey(e => new { e.IdNegocio, e.IdCita })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PagoHistorial_Cita");

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PagoHistorial_AspNetUsers");
        });
    }

    private static void ConfigureAuditoria(ModelBuilder builder)
    {
        builder.Entity<AuditoriaEvento>(entity =>
        {
            entity.ToTable("AuditoriaEvento");
            entity.HasKey(e => e.IdAuditoriaEvento).HasName("PK_AuditoriaEvento");
            entity.HasIndex(e => new { e.IdNegocio, e.FechaCreacion }).HasDatabaseName("IX_AuditoriaEvento_IdNegocio_FechaCreacion");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_AuditoriaEvento_UserId");
            entity.HasIndex(e => new { e.Entidad, e.EntidadId }).HasDatabaseName("IX_AuditoriaEvento_Entidad_EntidadId");
            entity.HasIndex(e => new { e.Categoria, e.Accion }).HasDatabaseName("IX_AuditoriaEvento_Categoria_Accion");

            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.Categoria).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Accion).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Entidad).HasMaxLength(120).IsRequired();
            entity.Property(e => e.EntidadId).HasMaxLength(128);
            entity.Property(e => e.Descripcion).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(e => e.Negocio)
                .WithMany(e => e.AuditoriaEventos)
                .HasForeignKey(e => e.IdNegocio)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AuditoriaEvento_Negocio");

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AuditoriaEvento_AspNetUsers");
        });
    }

    private static void ConfigureNotificaciones(ModelBuilder builder)
    {
        builder.Entity<Notificacion>(entity =>
        {
            entity.ToTable("Notificacion");
            entity.HasKey(e => e.IdNotificacion).HasName("PK_Notificacion");
            entity.HasIndex(e => e.IdNegocio).HasDatabaseName("IX_Notificacion_IdNegocio");
            entity.HasIndex(e => e.IdCita).HasDatabaseName("IX_Notificacion_IdCita");
            entity.HasIndex(e => e.IdResenaNegocio).HasDatabaseName("IX_Notificacion_IdResenaNegocio");
            entity.HasIndex(e => new { e.IdEstadoNotificacion, e.FechaProgramada }).HasDatabaseName("IX_Notificacion_Programada");
            entity.Property(e => e.Destinatario).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Asunto).HasMaxLength(200);
            entity.Property(e => e.Mensaje).IsRequired();
            entity.Property(e => e.Error).HasMaxLength(1000);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.HasOne(e => e.Negocio).WithMany(e => e.Notificaciones).HasForeignKey(e => e.IdNegocio).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Notificacion_Negocio");
            entity.HasOne(e => e.Cita).WithMany(e => e.Notificaciones).HasForeignKey(e => new { e.IdNegocio, e.IdCita }).HasPrincipalKey(e => new { e.IdNegocio, e.IdCita }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Notificacion_Cita");
            entity.HasOne(e => e.ResenaNegocio).WithMany(e => e.Notificaciones).HasForeignKey(e => e.IdResenaNegocio).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Notificacion_ResenaNegocio");
            entity.HasOne(e => e.TipoNotificacion).WithMany(e => e.Notificaciones).HasForeignKey(e => e.IdTipoNotificacion).HasConstraintName("FK_Notificacion_TipoNotificacion");
            entity.HasOne(e => e.CanalNotificacion).WithMany(e => e.Notificaciones).HasForeignKey(e => e.IdCanalNotificacion).HasConstraintName("FK_Notificacion_CanalNotificacion");
            entity.HasOne(e => e.EstadoNotificacion).WithMany(e => e.Notificaciones).HasForeignKey(e => e.IdEstadoNotificacion).HasConstraintName("FK_Notificacion_EstadoNotificacion");
        });
    }

    private static void ConfigureInvitaciones(ModelBuilder builder)
    {
        builder.Entity<InvitacionNegocio>(entity =>
        {
            entity.ToTable("InvitacionNegocio", table =>
            {
                table.HasCheckConstraint(
                    "CK_InvitacionNegocio_Estado",
                    "[Estado] IN ('Pendiente', 'Aceptada', 'Expirada', 'Cancelada', 'Reenviada')");
            });

            entity.HasKey(e => e.IdInvitacionNegocio).HasName("PK_InvitacionNegocio");
            entity.HasIndex(e => e.IdNegocio).HasDatabaseName("IX_InvitacionNegocio_IdNegocio");
            entity.HasIndex(e => e.NormalizedEmail).HasDatabaseName("IX_InvitacionNegocio_NormalizedEmail");
            entity.HasIndex(e => new { e.Estado, e.FechaExpiracion }).HasDatabaseName("IX_InvitacionNegocio_Estado_FechaExpiracion");
            entity.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("UQ_InvitacionNegocio_TokenHash");
            entity.HasIndex(e => new { e.IdNegocio, e.NormalizedEmail })
                .IsUnique()
                .HasFilter("[Estado] = 'Pendiente'")
                .HasDatabaseName("UQ_InvitacionNegocio_Pendiente_Email_Negocio");

            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.TokenHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(30).HasDefaultValue(InvitacionNegocioEstados.Pendiente).IsRequired();
            entity.Property(e => e.InvitadoPorUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.AceptadoPorUserId).HasMaxLength(128);
            entity.Property(e => e.CanceladoPorUserId).HasMaxLength(128);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.Mensaje).HasMaxLength(500);
            entity.Property(e => e.MotivoCancelacion).HasMaxLength(300);

            entity.HasOne(e => e.Negocio)
                .WithMany(e => e.InvitacionesNegocio)
                .HasForeignKey(e => e.IdNegocio)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_InvitacionNegocio_Negocio");

            entity.HasOne(e => e.RolNegocio)
                .WithMany(e => e.InvitacionesNegocio)
                .HasForeignKey(e => e.IdRolNegocio)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_InvitacionNegocio_RolNegocio");

            entity.HasOne(e => e.InvitadoPor)
                .WithMany()
                .HasForeignKey(e => e.InvitadoPorUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_InvitacionNegocio_InvitadoPor");

            entity.HasOne(e => e.AceptadoPor)
                .WithMany()
                .HasForeignKey(e => e.AceptadoPorUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_InvitacionNegocio_AceptadoPor");

            entity.HasOne(e => e.CanceladoPor)
                .WithMany()
                .HasForeignKey(e => e.CanceladoPorUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_InvitacionNegocio_CanceladoPor");
        });
    }

    private static void ConfigureAuthRefreshTokens(ModelBuilder builder)
    {
        builder.Entity<AuthRefreshToken>(entity =>
        {
            entity.ToTable("AuthRefreshToken");
            entity.HasKey(e => e.IdRefreshToken).HasName("PK_AuthRefreshToken");
            entity.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("UQ_AuthRefreshToken_TokenHash");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_AuthRefreshToken_UserId");
            entity.HasIndex(e => new { e.UserId, e.FechaExpiracion }).HasDatabaseName("IX_AuthRefreshToken_UserId_FechaExpiracion");

            entity.Property(e => e.UserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(e => e.ReemplazadoPorTokenHash).HasMaxLength(128);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_AuthRefreshToken_AspNetUsers_UserId");
        });
    }

    private static void ConfigureUbicaciones(ModelBuilder builder)
    {
        builder.Entity<Pais>(entity =>
        {
            entity.ToTable("Pais");
            entity.HasKey(e => e.IdPais).HasName("PK_Pais");
            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("UQ_Pais_Nombre");
            entity.HasIndex(e => e.CodigoIso2).IsUnique().HasDatabaseName("UQ_Pais_CodigoIso2");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CodigoIso2).HasMaxLength(2).IsRequired();
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        builder.Entity<Ciudad>(entity =>
        {
            entity.ToTable("Ciudad");
            entity.HasKey(e => e.IdCiudad).HasName("PK_Ciudad");
            entity.HasIndex(e => e.IdPais).HasDatabaseName("IX_Ciudad_IdPais");
            entity.HasIndex(e => new { e.IdPais, e.Nombre }).IsUnique().HasDatabaseName("UQ_Ciudad_Pais_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasOne(e => e.Pais)
                .WithMany(e => e.Ciudades)
                .HasForeignKey(e => e.IdPais)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Ciudad_Pais");
        });

        builder.Entity<Comuna>(entity =>
        {
            entity.ToTable("Comuna");
            entity.HasKey(e => e.IdComuna).HasName("PK_Comuna");
            entity.HasIndex(e => e.IdCiudad).HasDatabaseName("IX_Comuna_IdCiudad");
            entity.HasIndex(e => new { e.IdCiudad, e.Nombre }).IsUnique().HasDatabaseName("UQ_Comuna_Ciudad_Nombre");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasOne(e => e.Ciudad)
                .WithMany(e => e.Comunas)
                .HasForeignKey(e => e.IdCiudad)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Comuna_Ciudad");
        });
    }

    private static void ConfigureUsuariosPerfil(ModelBuilder builder)
    {
        builder.Entity<UsuarioPerfil>(entity =>
        {
            entity.ToTable("UsuarioPerfil");
            entity.HasKey(e => e.IdUsuarioPerfil).HasName("PK_UsuarioPerfil");
            entity.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("UQ_UsuarioPerfil_UserId");

            entity.Property(e => e.UserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.NombreCompleto).HasMaxLength(220);
            entity.Property(e => e.Rut).HasMaxLength(20);
            entity.Property(e => e.FechaNacimiento).HasColumnType("date");
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UsuarioPerfil_AspNetUsers_UserId");
        });

        builder.Entity<UsuarioContacto>(entity =>
        {
            entity.ToTable("UsuarioContacto");
            entity.HasKey(e => e.IdUsuarioContacto).HasName("PK_UsuarioContacto");
            entity.HasIndex(e => e.IdUsuarioPerfil).IsUnique().HasDatabaseName("UQ_UsuarioContacto_IdUsuarioPerfil");
            entity.Property(e => e.TelefonoAlternativo).HasMaxLength(30);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(e => e.UsuarioPerfil)
                .WithOne(e => e.Contacto)
                .HasForeignKey<UsuarioContacto>(e => e.IdUsuarioPerfil)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UsuarioContacto_UsuarioPerfil_IdUsuarioPerfil");
        });

        builder.Entity<UsuarioDireccion>(entity =>
        {
            entity.ToTable("UsuarioDireccion");
            entity.HasKey(e => e.IdUsuarioDireccion).HasName("PK_UsuarioDireccion");
            entity.HasIndex(e => e.IdUsuarioPerfil).IsUnique().HasDatabaseName("UQ_UsuarioDireccion_IdUsuarioPerfil");
            entity.HasIndex(e => e.IdComuna).HasDatabaseName("IX_UsuarioDireccion_IdComuna");
            entity.Property(e => e.Direccion).HasMaxLength(300);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(e => e.UsuarioPerfil)
                .WithOne(e => e.Direccion)
                .HasForeignKey<UsuarioDireccion>(e => e.IdUsuarioPerfil)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UsuarioDireccion_UsuarioPerfil_IdUsuarioPerfil");
            entity.HasOne(e => e.Comuna)
                .WithMany(e => e.UsuariosDireccion)
                .HasForeignKey(e => e.IdComuna)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UsuarioDireccion_Comuna_IdComuna");
        });

        builder.Entity<UsuarioConsentimiento>(entity =>
        {
            entity.ToTable("UsuarioConsentimiento");
            entity.HasKey(e => e.IdUsuarioConsentimiento).HasName("PK_UsuarioConsentimiento");
            entity.HasIndex(e => e.IdUsuarioPerfil).IsUnique().HasDatabaseName("UQ_UsuarioConsentimiento_IdUsuarioPerfil");
            entity.Property(e => e.AceptaTerminos).HasDefaultValue(false);
            entity.Property(e => e.AceptaMarketing).HasDefaultValue(false);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(e => e.UsuarioPerfil)
                .WithOne(e => e.Consentimiento)
                .HasForeignKey<UsuarioConsentimiento>(e => e.IdUsuarioPerfil)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UsuarioConsentimiento_UsuarioPerfil_IdUsuarioPerfil");
        });

        builder.Entity<UsuarioSeguridad>(entity =>
        {
            entity.ToTable("UsuarioSeguridad");
            entity.HasKey(e => e.IdUsuarioSeguridad).HasName("PK_UsuarioSeguridad");
            entity.HasIndex(e => e.IdUsuarioPerfil).IsUnique().HasDatabaseName("UQ_UsuarioSeguridad_IdUsuarioPerfil");
            entity.Property(e => e.DebeCambiarPassword).HasDefaultValue(false);
            entity.Property(e => e.IpUltimoLogin).HasMaxLength(45);
            entity.Property(e => e.UserAgentUltimoLogin).HasMaxLength(500);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(e => e.UsuarioPerfil)
                .WithOne(e => e.Seguridad)
                .HasForeignKey<UsuarioSeguridad>(e => e.IdUsuarioPerfil)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UsuarioSeguridad_UsuarioPerfil_IdUsuarioPerfil");
        });
    }
}
