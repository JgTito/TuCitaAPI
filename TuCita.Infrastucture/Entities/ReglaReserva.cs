using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class ReglaReserva
{
    public int IdReglaReserva { get; set; }
    public int IdNegocio { get; set; }
    public int MinHorasAnticipacion { get; set; } = 2;
    public int MaxDiasAdelanto { get; set; } = 30;
    public bool PermiteCancelacionCliente { get; set; } = true;
    public int HorasLimiteCancelacion { get; set; } = 6;
    public bool RequiereConfirmacionManual { get; set; }
    public bool PermiteSobreturnos { get; set; }
    public int MaxCitasActivasPorCliente { get; set; } = 1;
    public DateTime FechaActualizacion { get; set; }

    public Negocio Negocio { get; set; } = null!;
}

