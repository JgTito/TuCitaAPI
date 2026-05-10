using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class Negocio
{
    public int IdNegocio { get; set; }
    public int IdRubro { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? LogoUrl { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public Rubro Rubro { get; set; } = null!;
    public ICollection<NegocioUsuario> NegocioUsuarios { get; set; } = [];
    public ICollection<CategoriaServicio> CategoriasServicio { get; set; } = [];
    public ICollection<Servicio> Servicios { get; set; } = [];
    public ICollection<Prestador> Prestadores { get; set; } = [];
    public ICollection<PrestadorServicio> PrestadorServicios { get; set; } = [];
    public ICollection<HorarioNegocio> HorariosNegocio { get; set; } = [];
    public ICollection<HorarioPrestador> HorariosPrestador { get; set; } = [];
    public ICollection<BloqueoHorario> BloqueosHorario { get; set; } = [];
    public ICollection<Cliente> Clientes { get; set; } = [];
    public ICollection<CampoReserva> CamposReserva { get; set; } = [];
    public ICollection<Cita> Citas { get; set; } = [];
    public ICollection<ReglaReserva> ReglasReserva { get; set; } = [];
    public ICollection<Notificacion> Notificaciones { get; set; } = [];
    public ICollection<InvitacionNegocio> InvitacionesNegocio { get; set; } = [];
    public ICollection<Pago> Pagos { get; set; } = [];
    public ICollection<AuditoriaEvento> AuditoriaEventos { get; set; } = [];
    public ICollection<ResenaNegocio> Resenas { get; set; } = [];
    public ICollection<SolicitudResena> SolicitudesResena { get; set; } = [];
    public ICollection<ConfiguracionResenaNegocio> ConfiguracionesResena { get; set; } = [];
}
