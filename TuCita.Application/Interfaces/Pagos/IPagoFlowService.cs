using TuCita.Application.Common;

namespace TuCita.Application.Pagos;

public interface IPagoFlowService
{
    Task<ServiceResult<CrearPagoFlowResponseDto>> CrearPagoReservaPublicaAsync(
        CurrentUserContext currentUser,
        string slug,
        string codigo,
        CrearPagoReservaPublicaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagoFlowResultadoDto>> ConfirmarPagoAsync(
        string token,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagoFlowResultadoDto>> ProcesarRetornoAsync(
        string token,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagoFlowDto>> GetByCommerceOrderAsync(
        string commerceOrder,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagoNegocioListadoDto>> GetPagosNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagoNegocioDto>> RegistrarPagoManualAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        RegistrarPagoManualRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagoNegocioDto>> AnularPagoManualAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPago,
        AnularPagoManualRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagoNegocioDto>> RegistrarDevolucionAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPago,
        RegistrarDevolucionPagoRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<EstadoPagoFiltroDto>>> GetEstadosPagoSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<PagoProveedorFiltroDto>>> GetProveedoresSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<PagoOrigenFiltroDto>>> GetOrigenesSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<PagoMetodoFiltroDto>>> GetMetodosSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<PagoClienteFiltroDto>>> GetClientesSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<PagoServicioFiltroDto>>> GetServiciosSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<PagoCitaFiltroDto>>> GetCitasSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagoNegocioDto>> GetPagoNegocioByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPago,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<PagoHistorialDto>>> GetHistorialPagoAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPago,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagoComprobanteDto>> DescargarComprobanteNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPago,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaPagosDto>> GetPagosCitaNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagoComprobanteDto>> DescargarComprobanteMiCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        int idPago,
        CancellationToken cancellationToken);

    Task<ServiceResult<CitaPagosDto>> GetMisPagosCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagoComprobanteDto>> DescargarComprobanteReservaPublicaAsync(
        CurrentUserContext currentUser,
        string slug,
        string codigo,
        int idPago,
        DescargarComprobantePublicoRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProcesarPagosFlowPendientesResultDto>> ProcesarPendientesFlowAsync(
        int maxPagos,
        CancellationToken cancellationToken);

    Task<ServiceResult<ExpirarPagosPendientesResultDto>> ExpirarPendientesAsync(
        CancellationToken cancellationToken);
}
