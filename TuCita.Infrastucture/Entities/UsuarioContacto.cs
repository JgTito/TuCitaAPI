namespace TuCita.Infrastucture.Entities;

public sealed class UsuarioContacto
{
    public int IdUsuarioContacto { get; set; }
    public int IdUsuarioPerfil { get; set; }
    public string? TelefonoAlternativo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public UsuarioPerfil UsuarioPerfil { get; set; } = null!;
}
