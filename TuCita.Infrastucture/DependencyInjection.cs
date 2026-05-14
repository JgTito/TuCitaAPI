using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TuCita.Application.Agenda;
using TuCita.Application.Auditoria;
using TuCita.Application.Auth;
using TuCita.Application.BloqueosHorario;
using TuCita.Application.CampoReservaOpciones;
using TuCita.Application.CamposReserva;
using TuCita.Application.CanalesNotificacion;
using TuCita.Application.CategoriasServicio;
using TuCita.Application.CentroOperativo;
using TuCita.Application.Clientes;
using TuCita.Application.Citas;
using TuCita.Application.Dashboard;
using TuCita.Application.Disponibilidad;
using TuCita.Application.EstadosCita;
using TuCita.Application.EstadosNotificacion;
using TuCita.Application.HorariosNegocio;
using TuCita.Application.HorariosPrestador;
using TuCita.Application.InformesInteligentes;
using TuCita.Application.Invitaciones;
using TuCita.Application.Negocios;
using TuCita.Application.NegocioUsuarios;
using TuCita.Application.Notificaciones;
using TuCita.Application.Onboarding;
using TuCita.Application.Pagos;
using TuCita.Application.Prestadores;
using TuCita.Application.PrestadorServicios;
using TuCita.Application.ReglasReserva;
using TuCita.Application.Reportes;
using TuCita.Application.Resenas;
using TuCita.Application.ReservasPublicas;
using TuCita.Application.RolesNegocio;
using TuCita.Application.Rubros;
using TuCita.Application.Servicios;
using TuCita.Application.SuperAdminUsuarios;
using TuCita.Application.TiposCampo;
using TuCita.Application.TiposNotificacion;
using TuCita.Application.TiposPrestador;
using TuCita.Application.Ubicaciones;
using TuCita.Application.UsuariosPerfil;
using TuCita.Infrastucture.Agenda;
using TuCita.Infrastucture.Auditoria;
using TuCita.Infrastucture.Authentication;
using TuCita.Infrastucture.BloqueosHorario;
using TuCita.Infrastucture.CampoReservaOpciones;
using TuCita.Infrastucture.CamposReserva;
using TuCita.Infrastucture.CanalesNotificacion;
using TuCita.Infrastucture.CategoriasServicio;
using TuCita.Infrastucture.CentroOperativo;
using TuCita.Infrastucture.Clientes;
using TuCita.Infrastucture.Citas;
using TuCita.Infrastucture.Dashboard;
using TuCita.Infrastucture.Disponibilidad;
using TuCita.Infrastucture.Email;
using TuCita.Infrastucture.EstadosCita;
using TuCita.Infrastucture.EstadosNotificacion;
using TuCita.Infrastucture.HorariosNegocio;
using TuCita.Infrastucture.HorariosPrestador;
using TuCita.Infrastucture.InformesInteligentes;
using TuCita.Infrastucture.Invitaciones;
using TuCita.Infrastucture.Negocios;
using TuCita.Infrastucture.NegocioUsuarios;
using TuCita.Infrastucture.Notificaciones;
using TuCita.Infrastucture.Onboarding;
using TuCita.Infrastucture.Pagos;
using TuCita.Infrastucture.Persistence;
using TuCita.Infrastucture.Prestadores;
using TuCita.Infrastucture.PrestadorServicios;
using TuCita.Infrastucture.ReglasReserva;
using TuCita.Infrastucture.Reportes;
using TuCita.Infrastucture.Resenas;
using TuCita.Infrastucture.ReservasPublicas;
using TuCita.Infrastucture.RolesNegocio;
using TuCita.Infrastucture.Rubros;
using TuCita.Infrastucture.Servicios;
using TuCita.Infrastucture.SuperAdminUsuarios;
using TuCita.Infrastucture.TiposCampo;
using TuCita.Infrastucture.TiposNotificacion;
using TuCita.Infrastucture.TiposPrestador;
using TuCita.Infrastucture.Ubicaciones;
using TuCita.Infrastucture.UsuariosPerfil;

namespace TuCita.Infrastucture;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no está configurado.");

        services.AddDbContext<ReservaFlowDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services
            .AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ReservaFlowDbContext>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<FlowOptions>(configuration.GetSection(FlowOptions.SectionName));
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<INotificacionEmailTemplateRenderer, NotificacionEmailTemplateRenderer>();
        services.AddScoped<IFlowClient, FlowClient>();
        services.AddSingleton<IInformeInteligenteAiClient, GeminiInformeInteligenteClient>();

        services.AddScoped<IAgendaService, AgendaService>();
        services.AddScoped<IAuditoriaService, AuditoriaService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBloqueoHorarioService, BloqueoHorarioService>();
        services.AddScoped<ICampoReservaOpcionService, CampoReservaOpcionService>();
        services.AddScoped<ICampoReservaService, CampoReservaService>();
        services.AddScoped<ICanalNotificacionService, CanalNotificacionService>();
        services.AddScoped<ICategoriaServicioService, CategoriaServicioService>();
        services.AddScoped<ICentroOperativoService, CentroOperativoService>();
        services.AddScoped<IClienteResolverService, ClienteResolverService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<ICitaService, CitaService>();
        services.AddScoped<IDashboardNegocioService, DashboardNegocioService>();
        services.AddScoped<IDisponibilidadService, DisponibilidadService>();
        services.AddScoped<IEstadoCitaService, EstadoCitaService>();
        services.AddScoped<IEstadoNotificacionService, EstadoNotificacionService>();
        services.AddScoped<IHorarioNegocioService, HorarioNegocioService>();
        services.AddScoped<IHorarioPrestadorService, HorarioPrestadorService>();
        services.AddScoped<IInformeInteligenteService, InformeInteligenteService>();
        services.AddScoped<IInvitacionNegocioService, InvitacionNegocioService>();
        services.AddScoped<INegocioService, NegocioService>();
        services.AddScoped<INegocioUsuarioService, NegocioUsuarioService>();
        services.AddScoped<INotificacionService, NotificacionService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<ICitaPagoImpactService, CitaPagoImpactService>();
        services.AddScoped<IPagoFlowService, PagoFlowService>();
        services.AddScoped<IPrestadorService, PrestadorService>();
        services.AddScoped<IPrestadorServicioService, PrestadorServicioService>();
        services.AddScoped<IReglaReservaService, ReglaReservaService>();
        services.AddScoped<IReporteNegocioService, ReporteNegocioService>();
        services.AddScoped<IResenaNegocioService, ResenaNegocioService>();
        services.AddScoped<IReservaPublicaService, ReservaPublicaService>();
        services.AddScoped<IRubroService, RubroService>();
        services.AddScoped<IRolNegocioService, RolNegocioService>();
        services.AddScoped<IServicioService, ServicioService>();
        services.AddScoped<ISuperAdminUsuarioService, SuperAdminUsuarioService>();
        services.AddScoped<ITipoCampoService, TipoCampoService>();
        services.AddScoped<ITipoNotificacionService, TipoNotificacionService>();
        services.AddScoped<ITipoPrestadorService, TipoPrestadorService>();
        services.AddScoped<IUbicacionService, UbicacionService>();
        services.AddScoped<IUsuarioPerfilService, UsuarioPerfilService>();

        return services;
    }
}
