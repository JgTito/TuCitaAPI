using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Rubros;

public sealed class RubroSelectQuery
{
    [MaxLength(100)]
    public string? Search { get; init; }

    public bool SoloActivos { get; init; } = true;
}
