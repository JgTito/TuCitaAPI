using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Dashboard;

public sealed class DashboardPagosQuery
{
    public const int DefaultLimiteUltimosPagos = 10;

    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    [Range(1, 50)]
    public int? LimiteUltimosPagos { get; init; }
}
