using TuCita.Application.Common;

namespace TuCita.Application.Disponibilidad;

public interface IDisponibilidadService
{
    Task<ServiceResult<DisponibilidadDto>> GetDisponibilidadAsync(
        int idNegocio,
        DisponibilidadQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<DisponibilidadDto>> GetDisponibilidadAsync(
        int idNegocio,
        DisponibilidadQuery query,
        int? idCitaExcluir,
        CancellationToken cancellationToken);
}
