namespace TuCita.Application.Auditoria;

public sealed record AuditoriaCambioDto(
    string Campo,
    string? ValorAnterior,
    string? ValorNuevo);
