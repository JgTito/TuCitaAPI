using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Api.Authorization;

public sealed class ActiveUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ReservaFlowDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userStatus = await dbContext.Users
                    .AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => new
                    {
                        user.LockoutEnd,
                        PerfilInactivo = dbContext.UsuariosPerfil.Any(profile => profile.UserId == user.Id && !profile.Activo)
                    })
                    .FirstOrDefaultAsync(context.RequestAborted);

                var lockedOut = userStatus?.LockoutEnd.HasValue == true &&
                    userStatus.LockoutEnd > DateTimeOffset.UtcNow;

                if (userStatus is null || lockedOut || userStatus.PerfilInactivo)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
        }

        await next(context);
    }
}
