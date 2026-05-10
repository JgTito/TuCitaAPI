using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class BloqueoHorario
{
    public int IdBloqueoHorario { get; set; }
    public int IdNegocio { get; set; }
    public int? IdPrestador { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? Motivo { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public Prestador? Prestador { get; set; }
}

