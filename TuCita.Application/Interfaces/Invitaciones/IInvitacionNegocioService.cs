using TuCita.Application.Auth;
using TuCita.Application.Common;

namespace TuCita.Application.Invitaciones;

public interface IInvitacionNegocioService
{
    Task<PagedResult<InvitacionNegocioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        InvitacionNegocioQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<InvitacionNegocioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idInvitacion,
        CancellationToken cancellationToken);

    Task<PagedResult<InvitacionNegocioDto>> GetMineAsync(
        CurrentUserContext currentUser,
        MisInvitacionesQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<InvitacionNegocioDto>> GetMineByIdAsync(
        CurrentUserContext currentUser,
        int idInvitacion,
        CancellationToken cancellationToken);

    Task<ServiceResult<InvitacionCreadaDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateInvitacionNegocioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<InvitacionPreviewDto>> ValidateAsync(
        ValidateInvitacionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<InvitacionNegocioDto>> AcceptAsync(
        CurrentUserContext currentUser,
        AcceptInvitacionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<InvitacionNegocioDto>> AcceptMineAsync(
        CurrentUserContext currentUser,
        int idInvitacion,
        CancellationToken cancellationToken);

    Task<ServiceResult<AuthResponse>> RegisterAndAcceptAsync(
        RegisterAndAcceptInvitacionRequest request,
        string? avatarUrl,
        CancellationToken cancellationToken);

    Task<ServiceResult<InvitacionNegocioDto>> CancelAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idInvitacion,
        CancelInvitacionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<InvitacionCreadaDto>> ResendAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idInvitacion,
        CancellationToken cancellationToken);

    Task<ServiceResult<ExpirarInvitacionesResultDto>> ExpirarPendientesAsync(
        CancellationToken cancellationToken);
}
