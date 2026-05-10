using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Auditoria;

public sealed class AuditoriaQuery
{
    private const int MaxPageSize = 100;
    private int _pageNumber = 1;
    private int _pageSize = 20;

    [MaxLength(150)]
    public string? Search { get; init; }

    [MaxLength(80)]
    public string? Categoria { get; init; }

    [MaxLength(80)]
    public string? Accion { get; init; }

    [MaxLength(120)]
    public string? Entidad { get; init; }

    [MaxLength(128)]
    public string? EntidadId { get; init; }

    [MaxLength(128)]
    public string? UserId { get; init; }

    public int? IdNegocio { get; init; }

    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

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
