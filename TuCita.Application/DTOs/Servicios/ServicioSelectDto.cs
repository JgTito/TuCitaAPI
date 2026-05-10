namespace TuCita.Application.Servicios;

public sealed record ServicioSelectDto(
    int IdServicio,
    string Label,
    string Nombre,
    int? IdCategoriaServicio,
    string? CategoriaServicio,
    int DuracionMinutos,
    decimal Precio,
    bool RequiereProfesional,
    bool RequierePagoAnticipado,
    int TiempoPreparacionMinutos,
    bool Activo);
