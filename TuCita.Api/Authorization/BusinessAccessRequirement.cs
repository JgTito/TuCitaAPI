using Microsoft.AspNetCore.Authorization;

namespace TuCita.Api.Authorization;

public sealed class BusinessAccessRequirement(params string[] allowedRoles) : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> AllowedRoles { get; } = allowedRoles;
}
