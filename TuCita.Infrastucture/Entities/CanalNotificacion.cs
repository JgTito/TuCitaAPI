using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class CanalNotificacion
{
    public int IdCanalNotificacion { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Notificacion> Notificaciones { get; set; } = [];
}

