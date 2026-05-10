namespace TuCita.Application.ReglasReserva;

public sealed record ReglaReservaDto(
    int IdReglaReserva,
    int IdNegocio,
    string Negocio,
    int MinHorasAnticipacion,
    int MaxDiasAdelanto,
    bool PermiteCancelacionCliente,
    int HorasLimiteCancelacion,
    bool RequiereConfirmacionManual,
    bool PermiteSobreturnos,
    int MaxCitasActivasPorCliente,
    DateTime FechaActualizacion);
