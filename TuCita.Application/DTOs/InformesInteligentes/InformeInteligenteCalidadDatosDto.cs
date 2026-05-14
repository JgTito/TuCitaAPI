namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligenteCalidadDatosDto(
    bool TieneCitasSuficientes,
    int CitasSinPrestador,
    int PrestadoresSinHorario,
    IReadOnlyCollection<string> Advertencias);
