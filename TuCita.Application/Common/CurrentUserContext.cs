namespace TuCita.Application.Common;

public sealed record CurrentUserContext(
    string UserId,
    IReadOnlyCollection<string> Roles)
{
    public bool IsSuperAdmin => Roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
}
