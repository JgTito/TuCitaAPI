using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Dashboard;

public sealed class DashboardNegocioQuery
{
    public const int DefaultDiasProximasCitas = 7;
    public const int DefaultLimiteProximasCitas = 10;

    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    [Range(1, 90)]
    public int? DiasProximasCitas { get; init; }

    [Range(1, 50)]
    public int? LimiteProximasCitas { get; init; }

    [Range(1, 90)]
    public int? ProximasDias { get; init; }

    [Range(1, 50)]
    public int? CantidadProximas { get; init; }
}
