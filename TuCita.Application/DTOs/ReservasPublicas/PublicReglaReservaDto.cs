namespace TuCita.Application.ReservasPublicas;

public sealed record PublicReglaReservaDto(
    int MinHorasAnticipacion,
    int MaxDiasAdelanto,
    bool PermiteCancelacionCliente,
    int HorasLimiteCancelacion,
    bool RequiereConfirmacionManual,
    bool PermiteSobreturnos,
    int MaxCitasActivasPorCliente,
    DateTime FechaMinimaReserva,
    DateTime FechaMaximaReserva,
    DateTime? FechaActualizacion);
