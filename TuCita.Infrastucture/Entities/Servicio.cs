using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class Servicio
{
    public int IdServicio { get; set; }
    public int IdNegocio { get; set; }
    public int? IdCategoriaServicio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
    public bool RequiereProfesional { get; set; } = true;
    public bool RequierePagoAnticipado { get; set; }
    public int TiempoPreparacionMinutos { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public CategoriaServicio? CategoriaServicio { get; set; }
    public ICollection<PrestadorServicio> PrestadorServicios { get; set; } = [];
    public ICollection<CampoReserva> CamposReserva { get; set; } = [];
    public ICollection<Cita> Citas { get; set; } = [];
    public ICollection<ResenaNegocio> Resenas { get; set; } = [];
}

