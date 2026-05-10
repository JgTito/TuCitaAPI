namespace TuCita.Infrastucture.Entities;

public sealed class UsuarioConsentimiento
{
    public int IdUsuarioConsentimiento { get; set; }
    public int IdUsuarioPerfil { get; set; }
    public bool AceptaTerminos { get; set; }
    public DateTime? FechaAceptacionTerminos { get; set; }
    public bool AceptaMarketing { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public UsuarioPerfil UsuarioPerfil { get; set; } = null!;
}
