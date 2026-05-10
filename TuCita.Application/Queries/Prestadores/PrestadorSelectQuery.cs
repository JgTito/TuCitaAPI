using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Prestadores;

public sealed class PrestadorSelectQuery
{
    [MaxLength(150)]
    public string? Search { get; init; }

    public int? IdNegocio { get; init; }

    public int? IdTipoPrestador { get; init; }

    public int? IdServicio { get; init; }

    public bool SoloActivos { get; init; } = true;
}
