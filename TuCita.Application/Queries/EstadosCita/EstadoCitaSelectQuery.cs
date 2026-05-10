using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.EstadosCita;

public sealed class EstadoCitaSelectQuery
{
    [MaxLength(80)]
    public string? Search { get; init; }

    public bool? EsEstadoFinal { get; init; }

    public bool SoloActivos { get; init; } = true;
}
