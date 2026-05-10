using TuCita.Application.Common;
using TuCita.Application.Disponibilidad;

namespace TuCita.Application.Citas;

public interface ICitaService
{
    Task<PagedResult<CitaDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CitaQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CancellationToken cancellationToken);

    Task<ServiceResult<DisponibilidadDto>> GetDisponibilidadEdicionAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        DisponibilidadEdicionCitaQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaHistorialTimelineDto>> GetHistorialAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateCitaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        UpdateCitaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> ReagendarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        ReagendarCitaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> CambiarEstadoAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        ChangeEstadoCitaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> ConfirmarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> CancelarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> MarcarAtendidaAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> MarcarNoAsistioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken);

    Task<PagedResult<CitaDto>> GetMisCitasAsync(
        CurrentUserContext currentUser,
        CitaQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> GetMiCitaByIdAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaHistorialTimelineDto>> GetMiCitaHistorialAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> CancelarMiCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> ReagendarMiCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        ReagendarCitaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> GetMiAgendaCitaByIdAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> ReagendarMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        ReagendarCitaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> ConfirmarMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> CancelarMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> MarcarAtendidaMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> MarcarNoAsistioMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaDto>> ActualizarNotaInternaMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        UpdateNotaInternaCitaRequest request,
        CancellationToken cancellationToken);
}
