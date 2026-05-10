namespace TuCita.Application.ReservasPublicas;

public sealed record PublicServicioDto(
    int IdServicio,
    int? IdCategoriaServicio,
    string? CategoriaServicio,
    string Nombre,
    string? Descripcion,
    int DuracionMinutos,
    decimal Precio,
    bool RequiereProfesional,
    bool RequierePagoAnticipado,
    int TiempoPreparacionMinutos);
