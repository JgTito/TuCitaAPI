using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Pagos;

public sealed class PagoQuery
{
    private const int MaxPageSize = 100;
    private int _pageNumber = 1;
    private int _pageSize = 20;

    [MaxLength(150)]
    public string? Search { get; init; }

    public int? IdCita { get; init; }

    public int? IdCliente { get; init; }

    public int? IdServicio { get; init; }

    public int? IdEstadoPago { get; init; }

    [MaxLength(40)]
    public string? Proveedor { get; init; }

    public int? IdMetodoPago { get; init; }

    public bool? EsManual { get; init; }

    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    public DateTime? FechaPagoDesde { get; init; }

    public DateTime? FechaPagoHasta { get; init; }

    public bool? SoloPagados { get; init; }

    public bool? SoloPendientes { get; init; }

    [Range(1, int.MaxValue)]
    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    [Range(1, MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }
}
