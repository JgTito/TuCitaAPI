using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class RolNegocio
{
    public int IdRolNegocio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<NegocioUsuario> NegocioUsuarios { get; set; } = [];
    public ICollection<InvitacionNegocio> InvitacionesNegocio { get; set; } = [];
}
