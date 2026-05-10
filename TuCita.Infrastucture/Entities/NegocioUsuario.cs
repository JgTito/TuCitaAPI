using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class NegocioUsuario
{
    public int IdNegocioUsuario { get; set; }
    public int IdNegocio { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int IdRolNegocio { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public IdentityUser Usuario { get; set; } = null!;
    public RolNegocio RolNegocio { get; set; } = null!;
}

