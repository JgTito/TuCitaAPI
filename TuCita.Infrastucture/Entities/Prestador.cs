using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class Prestador
{
    public int IdPrestador { get; set; }
    public int IdNegocio { get; set; }
    public int IdTipoPrestador { get; set; }
    public string? UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public int Capacidad { get; set; } = 1;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public TipoPrestador TipoPrestador { get; set; } = null!;
    public IdentityUser? Usuario { get; set; }
    public ICollection<PrestadorServicio> PrestadorServicios { get; set; } = [];
    public ICollection<HorarioPrestador> HorariosPrestador { get; set; } = [];
    public ICollection<BloqueoHorario> BloqueosHorario { get; set; } = [];
    public ICollection<Cita> Citas { get; set; } = [];
    public ICollection<ResenaNegocio> Resenas { get; set; } = [];
}

