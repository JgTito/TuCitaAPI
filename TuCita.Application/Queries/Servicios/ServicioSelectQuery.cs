using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Servicios;

public sealed class ServicioSelectQuery
{
    [MaxLength(150)]
    public string? Search { get; init; }

    public int? IdCategoriaServicio { get; init; }

    public int? IdPrestador { get; init; }

    public bool? RequiereProfesional { get; init; }

    public bool? RequierePagoAnticipado { get; init; }

    public bool SoloActivos { get; init; } = true;
}
