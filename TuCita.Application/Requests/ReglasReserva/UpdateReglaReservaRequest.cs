using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.ReglasReserva;

public sealed record UpdateReglaReservaRequest(
    [Range(0, int.MaxValue)] int MinHorasAnticipacion,
    [Range(1, int.MaxValue)] int MaxDiasAdelanto,
    bool PermiteCancelacionCliente,
    [Range(0, int.MaxValue)] int HorasLimiteCancelacion,
    bool RequiereConfirmacionManual,
    bool PermiteSobreturnos,
    [Range(1, int.MaxValue)] int MaxCitasActivasPorCliente);
