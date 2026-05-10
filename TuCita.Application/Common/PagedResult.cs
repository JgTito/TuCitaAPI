namespace TuCita.Application.Common;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int PageSize,
    int TotalItems)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}
