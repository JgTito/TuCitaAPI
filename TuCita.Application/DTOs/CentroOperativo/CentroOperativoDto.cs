namespace TuCita.Application.CentroOperativo;

public sealed record CentroOperativoDto(
    int IdNegocio,
    string Negocio,
    DateTime FechaConsulta,
    CentroOperativoResumenDto Resumen,
    IReadOnlyCollection<CentroOperativoResenaBajaDto> ResenasBajas,
    IReadOnlyCollection<CentroOperativoPagoPendienteDto> PagosPendientes,
    IReadOnlyCollection<CentroOperativoCitaSinConfirmarDto> CitasSinConfirmar,
    IReadOnlyCollection<CentroOperativoInvitacionVencidaDto> InvitacionesVencidas,
    IReadOnlyCollection<CentroOperativoNotificacionErrorDto> NotificacionesConError,
    IReadOnlyCollection<CentroOperativoSolicitudCambioDto> SolicitudesCambio);
