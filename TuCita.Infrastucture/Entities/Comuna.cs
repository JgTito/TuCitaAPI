namespace TuCita.Infrastucture.Entities;

public sealed class Comuna
{
    public int IdComuna { get; set; }
    public int IdCiudad { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public Ciudad Ciudad { get; set; } = null!;
    public ICollection<UsuarioDireccion> UsuariosDireccion { get; set; } = [];
}
