using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class PrestadorServicio
{
    public int IdPrestadorServicio { get; set; }
    public int IdNegocio { get; set; }
    public int IdPrestador { get; set; }
    public int IdServicio { get; set; }
    public bool Activo { get; set; } = true;

    public Negocio Negocio { get; set; } = null!;
    public Prestador Prestador { get; set; } = null!;
    public Servicio Servicio { get; set; } = null!;
}

