using TuCita.Application.Common;

namespace TuCita.Application.Reportes;

public interface IReporteNegocioService
{
    Task<ServiceResult<ReporteExcelDto>> ExportExcelAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ReporteNegocioQuery query,
        CancellationToken cancellationToken);
}
