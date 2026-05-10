namespace TuCita.Application.CentroOperativo;

public sealed record CentroOperativoResumenDto(
    int TotalAcciones,
    int ResenasBajas,
    int PagosPendientes,
    int CitasSinConfirmar,
    int InvitacionesVencidas,
    int NotificacionesConError,
    int SolicitudesCambio,
    bool TieneAcciones);
