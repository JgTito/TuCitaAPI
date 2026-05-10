using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Clientes;
using TuCita.Application.Common;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/clientes")]
[Authorize]
public sealed class ClientesController(IClienteService clienteService) : TuCitaControllerBase
{
    [HttpGet]
    [Authorize(Policy = TuCitaPolicies.BusinessAgenda)]
    [ProducesResponseType(typeof(PagedResult<ClienteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClienteDto>>> GetAll(
        int idNegocio,
        [FromQuery] ClienteQuery query,
        CancellationToken cancellationToken)
    {
        var clientes = await clienteService.GetAllAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(clientes);
    }

    [HttpGet("select")]
    [Authorize(Policy = TuCitaPolicies.BusinessProfessional)]
    [ProducesResponseType(typeof(IReadOnlyCollection<ClienteSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<ClienteSelectDto>>> GetSelect(
        int idNegocio,
        [FromQuery] ClienteSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var clientes = await clienteService.GetSelectAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(clientes);
    }

    [HttpGet("{idCliente:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessAgenda)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> GetById(
        int idNegocio,
        int idCliente,
        CancellationToken cancellationToken)
    {
        var result = await clienteService.GetByIdAsync(GetCurrentUser(), idNegocio, idCliente, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = TuCitaPolicies.BusinessAgenda)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> Create(
        int idNegocio,
        CreateClienteRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await clienteService.CreateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idCliente = result.Data.IdCliente },
            result.Data);
    }

    [HttpPut("{idCliente:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessAgenda)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> Update(
        int idNegocio,
        int idCliente,
        UpdateClienteRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await clienteService.UpdateAsync(GetCurrentUser(), idNegocio, idCliente, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCliente:int}/activar")]
    [Authorize(Policy = TuCitaPolicies.BusinessAgenda)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> Activate(
        int idNegocio,
        int idCliente,
        CancellationToken cancellationToken)
    {
        var result = await clienteService.SetActiveAsync(GetCurrentUser(), idNegocio, idCliente, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCliente:int}/desactivar")]
    [Authorize(Policy = TuCitaPolicies.BusinessAgenda)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> Deactivate(
        int idNegocio,
        int idCliente,
        CancellationToken cancellationToken)
    {
        var result = await clienteService.SetActiveAsync(GetCurrentUser(), idNegocio, idCliente, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idCliente:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessAgenda)]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> Delete(
        int idNegocio,
        int idCliente,
        CancellationToken cancellationToken)
    {
        var result = await clienteService.DeleteAsync(GetCurrentUser(), idNegocio, idCliente, cancellationToken);
        return ToActionResult(result);
    }
}
