using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Reportes;

public sealed class ReporteNegocioQuery
{
    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    [Range(1, 100)]
    public int Top { get; init; } = 20;

    public bool IncluirDetalle { get; init; } = true;
}
