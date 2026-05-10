using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class UsuarioPerfil
{
    public int IdUsuarioPerfil { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? NombreCompleto { get; set; }
    public string? Rut { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? AvatarUrl { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public IdentityUser Usuario { get; set; } = null!;
    public UsuarioContacto? Contacto { get; set; }
    public UsuarioDireccion? Direccion { get; set; }
    public UsuarioConsentimiento? Consentimiento { get; set; }
    public UsuarioSeguridad? Seguridad { get; set; }
}
