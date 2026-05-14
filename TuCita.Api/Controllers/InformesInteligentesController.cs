using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Common;
using TuCita.Application.InformesInteligentes;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/informes-inteligentes")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class InformesInteligentesController(IInformeInteligenteService informeService) : TuCitaControllerBase
{
    [HttpGet("contexto")]
    [ProducesResponseType(typeof(InformeInteligenteContextoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InformeInteligenteContextoDto>> GetContexto(
        int idNegocio,
        [FromQuery] InformeInteligenteQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await informeService.GetContextoAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("descargar")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Descargar(
        int idNegocio,
        [FromQuery] InformeInteligenteQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await informeService.DescargarPdfAsync(
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
            ServiceResultStatus.Validation => ToValidationProblem<InformeInteligenteArchivoDto>(result.ValidationErrors).Result!,
            _ => BadRequest(result.Errors)
        };
    }
}
