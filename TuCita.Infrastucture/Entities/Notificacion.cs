using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class Notificacion
{
    public int IdNotificacion { get; set; }
    public int IdNegocio { get; set; }
    public int? IdCita { get; set; }
    public int? IdResenaNegocio { get; set; }
    public int IdTipoNotificacion { get; set; }
    public int IdCanalNotificacion { get; set; }
    public int IdEstadoNotificacion { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string? Asunto { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime? FechaProgramada { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public string? Error { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public Cita? Cita { get; set; }
    public ResenaNegocio? ResenaNegocio { get; set; }
    public TipoNotificacion TipoNotificacion { get; set; } = null!;
    public CanalNotificacion CanalNotificacion { get; set; } = null!;
    public EstadoNotificacion EstadoNotificacion { get; set; } = null!;
}

