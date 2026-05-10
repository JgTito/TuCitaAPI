using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Ubicaciones;

public sealed class PaisSelectQuery
{
    [MaxLength(100)]
    public string? Search { get; init; }

    public bool SoloActivos { get; init; } = true;
}
