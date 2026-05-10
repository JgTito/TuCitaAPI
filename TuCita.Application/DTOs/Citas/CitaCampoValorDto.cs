namespace TuCita.Application.Citas;

public sealed record CitaCampoValorDto(
    int IdCitaCampoValor,
    int IdCampoReserva,
    string CampoReserva,
    string NombreInterno,
    string? Valor);
