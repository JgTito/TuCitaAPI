using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TuCita.Application.Common;

namespace TuCita.Api.Controllers;

public abstract class TuCitaControllerBase : ControllerBase
{
    protected ActionResult<T> ToValidationProblem<T>(IReadOnlyCollection<string> errors)
    {
        return ToValidationProblem<T>(errors.Select(error => new ValidationError(string.Empty, error)).ToArray());
    }

    protected ActionResult<T> ToValidationProblem<T>(IReadOnlyCollection<ValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.Field, error.Message);
        }

        return ValidationProblem(ModelState);
    }
    protected ActionResult<T> ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return Ok(result.Data);
        }

        return result.Status switch
        {
            ServiceResultStatus.NotFound => NotFound(result.Errors),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Validation => ToValidationProblem<T>(result.ValidationErrors),
            _ => BadRequest(result.Errors)
        };
    }
    protected CurrentUserContext GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();

        return new CurrentUserContext(userId, roles);
    }
}
