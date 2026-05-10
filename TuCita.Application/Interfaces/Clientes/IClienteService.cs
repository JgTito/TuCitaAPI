using TuCita.Application.Common;

namespace TuCita.Application.Clientes;

public interface IClienteService
{
    Task<PagedResult<ClienteDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ClienteQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ClienteSelectDto>> GetSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ClienteSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<ClienteDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCliente,
        CancellationToken cancellationToken);

    Task<ServiceResult<ClienteDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateClienteRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ClienteDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCliente,
        UpdateClienteRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ClienteDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCliente,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<ClienteDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCliente,
        CancellationToken cancellationToken);
}
