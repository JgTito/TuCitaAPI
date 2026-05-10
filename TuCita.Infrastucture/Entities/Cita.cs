using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class Cita
{
    public int IdCita { get; set; }
    public int IdNegocio { get; set; }
    public int IdCliente { get; set; }
    public int IdServicio { get; set; }
    public int? IdPrestador { get; set; }
    public int IdEstadoCita { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? ComentarioCliente { get; set; }
    public string? NotaInterna { get; set; }
    public decimal PrecioEstimado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
    public Servicio Servicio { get; set; } = null!;
    public Prestador? Prestador { get; set; }
    public EstadoCita EstadoCita { get; set; } = null!;
    public ICollection<CitaCampoValor> CamposValor { get; set; } = [];
    public ICollection<CitaHistorial> Historial { get; set; } = [];
    public ICollection<Notificacion> Notificaciones { get; set; } = [];
    public ICollection<Pago> Pagos { get; set; } = [];
    public ICollection<ResenaNegocio> Resenas { get; set; } = [];
    public ICollection<SolicitudResena> SolicitudesResena { get; set; } = [];
}

