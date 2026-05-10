using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.ReglasReserva;

public sealed record CreateReglaReservaRequest(
    [Range(0, int.MaxValue)] int MinHorasAnticipacion = 2,
    [Range(1, int.MaxValue)] int MaxDiasAdelanto = 30,
    bool PermiteCancelacionCliente = true,
    [Range(0, int.MaxValue)] int HorasLimiteCancelacion = 6,
    bool RequiereConfirmacionManual = false,
    bool PermiteSobreturnos = false,
    [Range(1, int.MaxValue)] int MaxCitasActivasPorCliente = 1);
