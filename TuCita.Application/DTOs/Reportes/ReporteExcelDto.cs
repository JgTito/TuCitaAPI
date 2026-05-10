namespace TuCita.Application.Reportes;

public sealed record ReporteExcelDto(
    string FileName,
    string ContentType,
    byte[] Content);
