namespace TuCita.Application.Auditoria;

public sealed record AuditoriaEventoDto(
    int IdAuditoriaEvento,
    int? IdNegocio,
    string? Negocio,
    string? UserId,
    string? UsuarioEmail,
    string Categoria,
    string Accion,
    string Entidad,
    string? EntidadId,
    string Descripcion,
    IReadOnlyCollection<AuditoriaCambioDto> Cambios,
    string? ValoresAnterioresJson,
    string? ValoresNuevosJson,
    string? MetadataJson,
    DateTime FechaCreacion);
