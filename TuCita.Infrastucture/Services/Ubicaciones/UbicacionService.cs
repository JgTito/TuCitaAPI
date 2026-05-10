using Microsoft.EntityFrameworkCore;
using TuCita.Application.Ubicaciones;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Ubicaciones;

public sealed class UbicacionService(ReservaFlowDbContext dbContext) : IUbicacionService
{
    public async Task<IReadOnlyCollection<PaisSelectDto>> GetPaisesSelectAsync(
        PaisSelectQuery query,
        CancellationToken cancellationToken)
    {
        var paisesQuery = dbContext.Paises.AsNoTracking().AsQueryable();

        if (query.SoloActivos)
        {
            paisesQuery = paisesQuery.Where(item => item.Activo);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            paisesQuery = paisesQuery.Where(item =>
                item.Nombre.Contains(search) ||
                item.CodigoIso2.Contains(search));
        }

        return await paisesQuery
            .OrderBy(item => item.Nombre)
            .Select(item => new PaisSelectDto(item.IdPais, item.Nombre, item.CodigoIso2))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CiudadSelectDto>> GetCiudadesSelectAsync(
        CiudadSelectQuery query,
        CancellationToken cancellationToken)
    {
        var ciudadesQuery = dbContext.Ciudades.AsNoTracking().AsQueryable();

        if (query.SoloActivos)
        {
            ciudadesQuery = ciudadesQuery.Where(item => item.Activo && item.Pais.Activo);
        }

        if (query.IdPais.HasValue)
        {
            ciudadesQuery = ciudadesQuery.Where(item => item.IdPais == query.IdPais.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            ciudadesQuery = ciudadesQuery.Where(item => item.Nombre.Contains(search));
        }

        return await ciudadesQuery
            .OrderBy(item => item.Nombre)
            .Select(item => new CiudadSelectDto(item.IdCiudad, item.IdPais, item.Nombre))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ComunaSelectDto>> GetComunasSelectAsync(
        ComunaSelectQuery query,
        CancellationToken cancellationToken)
    {
        var comunasQuery = dbContext.Comunas.AsNoTracking().AsQueryable();

        if (query.SoloActivos)
        {
            comunasQuery = comunasQuery.Where(item =>
                item.Activo &&
                item.Ciudad.Activo &&
                item.Ciudad.Pais.Activo);
        }

        if (query.IdCiudad.HasValue)
        {
            comunasQuery = comunasQuery.Where(item => item.IdCiudad == query.IdCiudad.Value);
        }

        if (query.IdPais.HasValue)
        {
            comunasQuery = comunasQuery.Where(item => item.Ciudad.IdPais == query.IdPais.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            comunasQuery = comunasQuery.Where(item => item.Nombre.Contains(search));
        }

        return await comunasQuery
            .OrderBy(item => item.Nombre)
            .Select(item => new ComunaSelectDto(item.IdComuna, item.IdCiudad, item.Nombre))
            .ToArrayAsync(cancellationToken);
    }
}
