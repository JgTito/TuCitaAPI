namespace TuCita.Infrastucture.Entities;

public sealed class UsuarioDireccion
{
    public int IdUsuarioDireccion { get; set; }
    public int IdUsuarioPerfil { get; set; }
    public int? IdComuna { get; set; }
    public string? Direccion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public UsuarioPerfil UsuarioPerfil { get; set; } = null!;
    public Comuna? Comuna { get; set; }
}
