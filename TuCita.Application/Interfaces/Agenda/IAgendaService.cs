using TuCita.Application.Common;

namespace TuCita.Application.Agenda;

public interface IAgendaService
{
    Task<ServiceResult<MiAgendaDto>> GetMiAgendaAsync(
        CurrentUserContext currentUser,
        MiAgendaQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<MovimientosDisponiblesDto>> GetMovimientosDisponiblesAsync(
        CurrentUserContext currentUser,
        int idCita,
        MovimientosDisponiblesQuery query,
        CancellationToken cancellationToken);
}
