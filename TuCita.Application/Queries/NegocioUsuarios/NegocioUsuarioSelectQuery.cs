using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.NegocioUsuarios;

public sealed class NegocioUsuarioSelectQuery
{
    [MaxLength(100)]
    public string? Search { get; init; }

    public int? IdRolNegocio { get; init; }

    public bool SoloActivos { get; init; } = true;
}
