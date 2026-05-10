using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class HorarioNegocio
{
    public int IdHorarioNegocio { get; set; }
    public int IdNegocio { get; set; }
    public byte DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public bool Activo { get; set; } = true;

    public Negocio Negocio { get; set; } = null!;
}

