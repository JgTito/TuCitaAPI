using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class CitaHistorial
{
    public int IdCitaHistorial { get; set; }
    public int IdNegocio { get; set; }
    public int IdCita { get; set; }
    public int? IdEstadoAnterior { get; set; }
    public int IdEstadoNuevo { get; set; }
    public string? UserId { get; set; }
    public string? Observacion { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Cita Cita { get; set; } = null!;
    public EstadoCita? EstadoAnterior { get; set; }
    public EstadoCita EstadoNuevo { get; set; } = null!;
    public IdentityUser? Usuario { get; set; }
}

