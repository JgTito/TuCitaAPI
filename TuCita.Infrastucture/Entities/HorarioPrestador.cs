using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class HorarioPrestador
{
    public int IdHorarioPrestador { get; set; }
    public int IdNegocio { get; set; }
    public int IdPrestador { get; set; }
    public byte DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public bool Activo { get; set; } = true;

    public Negocio Negocio { get; set; } = null!;
    public Prestador Prestador { get; set; } = null!;
}

