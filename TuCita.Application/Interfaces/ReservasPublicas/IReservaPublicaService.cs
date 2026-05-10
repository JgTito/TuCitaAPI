using TuCita.Application.Common;
using TuCita.Application.Disponibilidad;

namespace TuCita.Application.ReservasPublicas;

public interface IReservaPublicaService
{
    Task<PagedResult<PublicNegocioSearchDto>> SearchNegociosAsync(
        PublicNegocioQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<PublicNegocioDto>> GetNegocioAsync(
        string slug,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<PublicServicioDto>>> GetServiciosAsync(
        string slug,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<PublicPrestadorDto>>> GetPrestadoresAsync(
        string slug,
        int idServicio,
        CancellationToken cancellationToken);

    Task<ServiceResult<DisponibilidadDto>> GetDisponibilidadAsync(
        string slug,
        DisponibilidadQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<PublicCampoReservaDto>>> GetCamposReservaAsync(
        string slug,
        int? idServicio,
        CancellationToken cancellationToken);

    Task<ServiceResult<PublicReglaReservaDto>> GetReglasReservaAsync(
        string slug,
        CancellationToken cancellationToken);

    Task<ServiceResult<PublicReservaDto>> GetReservaByCodigoAsync(
        string slug,
        string codigo,
        CancellationToken cancellationToken);

    Task<ServiceResult<PublicReservaMisDatosDto>> GetMisDatosReservaAsync(
        CurrentUserContext currentUser,
        string slug,
        CancellationToken cancellationToken);

    Task<ServiceResult<PublicReservaDto>> CreateReservaAsync(
        CurrentUserContext currentUser,
        string slug,
        CreateReservaPublicaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PublicReservaDto>> CancelReservaAsync(
        string slug,
        string codigo,
        CancelReservaPublicaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PublicReservaDto>> ReagendarReservaAsync(
        string slug,
        string codigo,
        ReagendarReservaPublicaRequest request,
        CancellationToken cancellationToken);
}
