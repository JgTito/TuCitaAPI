using TuCita.Application.Common;

namespace TuCita.Application.Pagos;

public sealed record PagoNegocioListadoDto(
    PagoNegocioResumenDto Resumen,
    PagedResult<PagoNegocioDto> Pagos);
