using TuCita.Application.Common;

namespace TuCita.Application.Resenas;

public interface IResenaNegocioService
{
    Task<ServiceResult<PagedResult<ResenaNegocioDto>>> GetByNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ResenaNegocioQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<ResenaResumenDto>> GetResumenNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken);

    Task<ServiceResult<ReputacionNegocioDto>> GetReputacionAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ReputacionNegocioQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<ResenaEstadoSelectDto>>> GetEstadosSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<ResenaPuntuacionSelectDto>>> GetPuntuacionesSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken);

    Task<ServiceResult<ConfiguracionResenaNegocioDto>> GetConfiguracionAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken);

    Task<ServiceResult<ConfiguracionResenaNegocioDto>> UpdateConfiguracionAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        UpdateConfiguracionResenaNegocioRequest request,
        CancellationToken cancellationToken);

    Task<PagedResult<ResenaPublicaDto>> GetPublicasAsync(
        string slug,
        ResenaPublicaQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<ResenaResumenDto>> GetResumenPublicoAsync(
        string slug,
        CancellationToken cancellationToken);

    Task<ServiceResult<ResenaNegocioDto>> GetMiResenaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken);

    Task<ServiceResult<ResenaNegocioDto>> CreateMiResenaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CrearResenaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ResenaNegocioDto>> UpdateMiResenaAsync(
        CurrentUserContext currentUser,
        int idCita,
        ActualizarResenaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<SolicitudResenaPreviewDto>> ValidarSolicitudAsync(
        ValidarSolicitudResenaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ResenaPublicaCreadaDto>> CreatePublicaAsync(
        CrearResenaPublicaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ResenaNegocioDto>> AprobarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idResena,
        ModerarResenaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ResenaNegocioDto>> RechazarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idResena,
        ModerarResenaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ResenaNegocioDto>> OcultarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idResena,
        ModerarResenaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ResenaNegocioDto>> ResponderAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idResena,
        ResponderResenaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CrearSolicitudResenaResultDto>> CrearSolicitudPostAtencionAsync(
        int idCita,
        CancellationToken cancellationToken);

    Task<ServiceResult<ExpirarSolicitudesResenaResultDto>> ExpirarSolicitudesPendientesAsync(
        CancellationToken cancellationToken);

    Task CancelarSolicitudesPendientesCitaAsync(
        int idCita,
        CancellationToken cancellationToken);
}
