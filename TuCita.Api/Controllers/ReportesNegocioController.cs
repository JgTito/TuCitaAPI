using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Common;
using TuCita.Application.Reportes;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/reportes")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class ReportesNegocioController(IReporteNegocioService reporteNegocioService) : TuCitaControllerBase
{
    [HttpGet("excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportExcel(
        int idNegocio,
        [FromQuery] ReporteNegocioQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await reporteNegocioService.ExportExcelAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        if (result.Succeeded && result.Data is not null)
        {
            return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
        }

        return result.Status switch
        {
            ServiceResultStatus.NotFound => NotFound(result.Errors),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Validation => ToValidationProblem(result.ValidationErrors),
            _ => BadRequest(result.Errors)
        };
    }

    private IActionResult ToValidationProblem(IReadOnlyCollection<ValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.Field, error.Message);
        }

        return ValidationProblem(ModelState);
    }
}
