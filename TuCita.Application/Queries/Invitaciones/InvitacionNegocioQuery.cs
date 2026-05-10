using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Invitaciones;

public sealed class InvitacionNegocioQuery
{
    private const int MaxPageSize = 100;
    private int _pageNumber = 1;
    private int _pageSize = 20;

    [MaxLength(30)]
    public string? Estado { get; init; }

    [MaxLength(256)]
    public string? Email { get; init; }

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
