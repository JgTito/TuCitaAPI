using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Ubicaciones;

public sealed class CiudadSelectQuery
{
    public int? IdPais { get; init; }

    [MaxLength(100)]
    public string? Search { get; init; }

    public bool SoloActivos { get; init; } = true;
}
