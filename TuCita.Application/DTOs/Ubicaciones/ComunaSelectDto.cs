namespace TuCita.Application.Ubicaciones;

public sealed record ComunaSelectDto(
    int IdComuna,
    int IdCiudad,
    string Nombre);
