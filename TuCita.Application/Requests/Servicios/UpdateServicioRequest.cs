using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Servicios;

public sealed record UpdateServicioRequest(
    int? IdCategoriaServicio,
    [Required, MaxLength(150)] string Nombre,
    [MaxLength(500)] string? Descripcion,
    [Range(1, int.MaxValue)] int DuracionMinutos,
    [Range(typeof(decimal), "0", "9999999999999999")] decimal Precio,
    bool RequiereProfesional,
    bool RequierePagoAnticipado,
    [Range(0, int.MaxValue)] int TiempoPreparacionMinutos,
    bool Activo);
