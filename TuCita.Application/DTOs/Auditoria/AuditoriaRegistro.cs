namespace TuCita.Application.Auditoria;

public sealed record AuditoriaRegistro(
    int? IdNegocio,
    string Categoria,
    string Accion,
    string Entidad,
    string? EntidadId,
    string Descripcion,
    object? ValoresAnteriores = null,
    object? ValoresNuevos = null,
    object? Metadata = null);
