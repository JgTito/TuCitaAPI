using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.TiposCampo;

public sealed class TipoCampoSelectQuery
{
    [MaxLength(80)]
    public string? Search { get; init; }

    public bool SoloActivos { get; init; } = true;
}
