using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class TipoPrestador
{
    public int IdTipoPrestador { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Prestador> Prestadores { get; set; } = [];
}

