using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.CentroOperativo;

public sealed class CentroOperativoQuery
{
    [Range(1, 50)]
    public int LimitePorCategoria { get; init; } = 10;

    [Range(1, 90)]
    public int DiasProximasCitas { get; init; } = 14;

    [Range(1, 365)]
    public int DiasErroresNotificaciones { get; init; } = 30;

    [Range(1, 365)]
    public int DiasSolicitudesCambio { get; init; } = 30;
}
