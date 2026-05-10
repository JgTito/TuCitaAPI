namespace TuCita.Application.Ubicaciones;

public sealed record CiudadSelectDto(
    int IdCiudad,
    int IdPais,
    string Nombre);
