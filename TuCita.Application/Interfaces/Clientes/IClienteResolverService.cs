using TuCita.Application.Common;

namespace TuCita.Application.Clientes;

public interface IClienteResolverService
{
    Task<ServiceResult<ClienteReservaDto>> ResolveForReservaPublicaAsync(
        CurrentUserContext currentUser,
        ResolveClienteReservaRequest request,
        CancellationToken cancellationToken);
}
