using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class CategoriaServicio
{
    public int IdCategoriaServicio { get; set; }
    public int IdNegocio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    public Negocio Negocio { get; set; } = null!;
    public ICollection<Servicio> Servicios { get; set; } = [];
}

