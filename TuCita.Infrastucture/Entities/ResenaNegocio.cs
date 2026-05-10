using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class ResenaNegocio
{
    public int IdResenaNegocio { get; set; }
    public int IdNegocio { get; set; }
    public int IdCita { get; set; }
    public int IdCliente { get; set; }
    public string? UserId { get; set; }
    public int IdServicio { get; set; }
    public int? IdPrestador { get; set; }
    public byte Puntuacion { get; set; }
    public string? Comentario { get; set; }
    public string Estado { get; set; } = ResenaNegocioEstados.Pendiente;
    public bool EsVisiblePublicamente { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public string? ModeradoPorUserId { get; set; }
    public DateTime? FechaModeracion { get; set; }
    public string? MotivoModeracion { get; set; }
    public string? RespuestaNegocio { get; set; }
    public string? RespondidoPorUserId { get; set; }
    public DateTime? FechaRespuesta { get; set; }
    public bool EsAlertaOperativa { get; set; }
    public DateTime? FechaAlertaOperativa { get; set; }
    public string? MotivoAlertaOperativa { get; set; }
    public bool Activo { get; set; } = true;
    public string ClienteNombreSnapshot { get; set; } = string.Empty;
    public string ServicioNombreSnapshot { get; set; } = string.Empty;
    public string? PrestadorNombreSnapshot { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public Cita Cita { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
    public IdentityUser? Usuario { get; set; }
    public Servicio Servicio { get; set; } = null!;
    public Prestador? Prestador { get; set; }
    public IdentityUser? ModeradoPor { get; set; }
    public IdentityUser? RespondidoPor { get; set; }
    public ICollection<Notificacion> Notificaciones { get; set; } = [];
}

public static class ResenaNegocioEstados
{
    public const string Pendiente = "Pendiente";
    public const string Aprobada = "Aprobada";
    public const string Rechazada = "Rechazada";
    public const string Oculta = "Oculta";
}
