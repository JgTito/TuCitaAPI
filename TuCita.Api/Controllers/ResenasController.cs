using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Resenas;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/resenas")]
[AllowAnonymous]
public sealed class ResenasController(IResenaNegocioService resenaService) : TuCitaControllerBase
{
    [HttpPost("validar-token")]
    [ProducesResponseType(typeof(SolicitudResenaPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SolicitudResenaPreviewDto>> ValidarToken(
        ValidarSolicitudResenaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.ValidarSolicitudAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("publicas")]
    [ProducesResponseType(typeof(ResenaPublicaCreadaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResenaPublicaCreadaDto>> CrearPublica(
        CrearResenaPublicaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.CreatePublicaAsync(request, cancellationToken);
        return ToActionResult(result);
    }
}
