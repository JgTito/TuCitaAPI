using TuCita.Application.Common;

namespace TuCita.Application.CentroOperativo;

public interface ICentroOperativoService
{
    Task<ServiceResult<CentroOperativoDto>> GetAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CentroOperativoQuery query,
        CancellationToken cancellationToken);
}
