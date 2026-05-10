using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Api.Authorization;

public sealed class BusinessAccessHandler(ReservaFlowDbContext dbContext)
    : AuthorizationHandler<BusinessAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BusinessAccessRequirement requirement)
    {
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var idNegocio = GetRouteIdNegocio(context.Resource);

        if (string.IsNullOrWhiteSpace(userId) || !idNegocio.HasValue)
        {
            return;
        }

        var hasAccess = await dbContext.NegocioUsuarios.AnyAsync(item =>
            item.IdNegocio == idNegocio.Value &&
            item.UserId == userId &&
            item.Activo &&
            requirement.AllowedRoles.Contains(item.RolNegocio.Nombre));

        if (hasAccess)
        {
            context.Succeed(requirement);
        }
    }

    private static int? GetRouteIdNegocio(object? resource)
    {
        if (resource is HttpContext httpContext &&
            httpContext.GetRouteValue("idNegocio") is { } value &&
            int.TryParse(value.ToString(), out var idNegocio))
        {
            return idNegocio;
        }

        return null;
    }
}
