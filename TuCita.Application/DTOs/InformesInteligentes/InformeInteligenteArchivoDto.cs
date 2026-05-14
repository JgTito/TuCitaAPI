namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligenteArchivoDto(
    string FileName,
    string ContentType,
    byte[] Content);
