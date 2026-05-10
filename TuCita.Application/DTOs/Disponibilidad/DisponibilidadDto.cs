namespace TuCita.Application.Disponibilidad;

public sealed record DisponibilidadDto(
    int IdNegocio,
    int IdServicio,
    string Servicio,
    DateOnly Fecha,
    int DuracionMinutos,
    int TiempoPreparacionMinutos,
    int IntervaloMinutos,
    IReadOnlyCollection<DisponibilidadSlotDto> Slots);
