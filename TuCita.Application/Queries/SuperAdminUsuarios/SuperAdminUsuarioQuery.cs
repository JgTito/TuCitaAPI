using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.SuperAdminUsuarios;

public sealed class SuperAdminUsuarioQuery
{
    private const int MaxPageSize = 100;
    private int _pageNumber = 1;
    private int _pageSize = 20;

    [MaxLength(256)]
    public string? Search { get; init; }

    [MaxLength(256)]
    public string? Rol { get; init; }

    public bool? Activo { get; init; }

    public bool? EmailConfirmado { get; init; }

    public bool? TieneNegocios { get; init; }

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
