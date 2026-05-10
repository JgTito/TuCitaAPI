using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Pagos;

public sealed class PagoFiltroSelectQuery
{
    private const int MaxPageSize = 200;
    private int _take = 100;

    [MaxLength(150)]
    public string? Search { get; init; }

    [Range(1, MaxPageSize)]
    public int Take
    {
        get => _take;
        init => _take = Math.Clamp(value, 1, MaxPageSize);
    }
}
