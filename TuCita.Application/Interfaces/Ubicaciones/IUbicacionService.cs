namespace TuCita.Application.Ubicaciones;

public interface IUbicacionService
{
    Task<IReadOnlyCollection<PaisSelectDto>> GetPaisesSelectAsync(
        PaisSelectQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CiudadSelectDto>> GetCiudadesSelectAsync(
        CiudadSelectQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ComunaSelectDto>> GetComunasSelectAsync(
        ComunaSelectQuery query,
        CancellationToken cancellationToken);
}
