namespace TuCita.Infrastucture.Entities;

public sealed class UsuarioSeguridad
{
    public int IdUsuarioSeguridad { get; set; }
    public int IdUsuarioPerfil { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public bool DebeCambiarPassword { get; set; }
    public DateTime? FechaUltimoCambioPassword { get; set; }
    public DateTime? FechaUltimoLogin { get; set; }
    public string? IpUltimoLogin { get; set; }
    public string? UserAgentUltimoLogin { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public UsuarioPerfil UsuarioPerfil { get; set; } = null!;
}
