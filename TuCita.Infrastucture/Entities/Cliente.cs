using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class Cliente
{
    public int IdCliente { get; set; }
    public int IdNegocio { get; set; }
    public string? UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Rut { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public IdentityUser? Usuario { get; set; }
    public ICollection<Cita> Citas { get; set; } = [];
    public ICollection<ResenaNegocio> Resenas { get; set; } = [];
    public ICollection<SolicitudResena> SolicitudesResena { get; set; } = [];
}

