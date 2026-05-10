using TuCita.Application.Common;

namespace TuCita.Application.EstadosCita;

public interface IEstadoCitaService
{
    Task<PagedResult<EstadoCitaDto>> GetAllAsync(EstadoCitaQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EstadoCitaSelectDto>> GetSelectAsync(EstadoCitaSelectQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<EstadoCitaDto>> GetByIdAsync(int idEstadoCita, CancellationToken cancellationToken);

    Task<ServiceResult<EstadoCitaDto>> CreateAsync(CurrentUserContext currentUser, CreateEstadoCitaRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<EstadoCitaDto>> UpdateAsync(CurrentUserContext currentUser, int idEstadoCita, UpdateEstadoCitaRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<EstadoCitaDto>> SetActiveAsync(CurrentUserContext currentUser, int idEstadoCita, bool activo, CancellationToken cancellationToken);

    Task<ServiceResult<EstadoCitaDto>> DeleteAsync(CurrentUserContext currentUser, int idEstadoCita, CancellationToken cancellationToken);
}
