using TuCita.Application.Common;

namespace TuCita.Application.InformesInteligentes;

public interface IInformeInteligenteService
{
    Task<ServiceResult<InformeInteligenteContextoDto>> GetContextoAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        InformeInteligenteQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<InformeInteligenteArchivoDto>> DescargarPdfAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        InformeInteligenteQuery query,
        CancellationToken cancellationToken);
}
