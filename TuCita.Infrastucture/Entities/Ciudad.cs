namespace TuCita.Infrastucture.Entities;

public sealed class Ciudad
{
    public int IdCiudad { get; set; }
    public int IdPais { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public Pais Pais { get; set; } = null!;
    public ICollection<Comuna> Comunas { get; set; } = [];
}
