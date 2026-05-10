using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.CategoriasServicio;
using TuCita.Application.Common;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.CategoriasServicio;

public sealed class CategoriaServicioService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : ICategoriaServicioService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";

    public async Task<PagedResult<CategoriaServicioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CategoriaServicioQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<CategoriaServicioDto>([], query.PageNumber, query.PageSize, 0);
        }

        var categoriasQuery = BaseQuery(idNegocio).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            categoriasQuery = categoriasQuery.Where(categoria =>
                categoria.Nombre.Contains(search) ||
                (categoria.Descripcion != null && categoria.Descripcion.Contains(search)));
        }

        if (query.Activo.HasValue)
        {
            categoriasQuery = categoriasQuery.Where(categoria => categoria.Activo == query.Activo.Value);
        }

        var totalItems = await categoriasQuery.CountAsync(cancellationToken);
        var items = await categoriasQuery
            .OrderBy(categoria => categoria.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(categoria => new CategoriaServicioDto(
                categoria.IdCategoriaServicio,
                categoria.IdNegocio,
                categoria.Negocio.Nombre,
                categoria.Nombre,
                categoria.Descripcion,
                categoria.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<CategoriaServicioDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<CategoriaServicioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCategoriaServicio,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var categoria = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdCategoriaServicio == idCategoriaServicio, cancellationToken);

        return categoria is null
            ? ServiceResult<CategoriaServicioDto>.NotFound("La categoría de servicio no existe.")
            : ServiceResult<CategoriaServicioDto>.Success(ToDto(categoria));
    }

    public async Task<ServiceResult<CategoriaServicioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateCategoriaServicioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var validationErrors = await ValidateNombreAsync(idNegocio, request.Nombre, null, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<CategoriaServicioDto>.Validation(validationErrors);
        }

        var categoria = new CategoriaServicio
        {
            IdNegocio = idNegocio,
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Activo = request.Activo
        };

        dbContext.CategoriasServicio.Add(categoria);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCategoriaServicio == categoria.IdCategoriaServicio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Servicios",
                "CrearCategoria",
                nameof(CategoriaServicio),
                created.IdCategoriaServicio.ToString(),
                $"Categoría de servicio creada: {created.Nombre}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CategoriaServicioDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<CategoriaServicioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCategoriaServicio,
        UpdateCategoriaServicioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var categoria = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdCategoriaServicio == idCategoriaServicio, cancellationToken);

        if (categoria is null)
        {
            return ServiceResult<CategoriaServicioDto>.NotFound("La categoría de servicio no existe.");
        }

        var validationErrors = await ValidateNombreAsync(idNegocio, request.Nombre, idCategoriaServicio, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<CategoriaServicioDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(categoria);

        categoria.Nombre = request.Nombre.Trim();
        categoria.Descripcion = request.Descripcion?.Trim();
        categoria.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCategoriaServicio == idCategoriaServicio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Servicios",
                "EditarCategoria",
                nameof(CategoriaServicio),
                idCategoriaServicio.ToString(),
                $"Categoría de servicio editada: {updated.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CategoriaServicioDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<CategoriaServicioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCategoriaServicio,
        bool activo,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var categoria = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdCategoriaServicio == idCategoriaServicio, cancellationToken);

        if (categoria is null)
        {
            return ServiceResult<CategoriaServicioDto>.NotFound("La categoría de servicio no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(categoria);

        categoria.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCategoriaServicio == idCategoriaServicio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Servicios",
                activo ? "ActivarCategoria" : "DesactivarCategoria",
                nameof(CategoriaServicio),
                idCategoriaServicio.ToString(),
                activo
                    ? $"Categoría de servicio activada: {updated.Nombre}"
                    : $"Categoría de servicio desactivada: {updated.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CategoriaServicioDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<CategoriaServicioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCategoriaServicio,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, idCategoriaServicio, activo: false, cancellationToken);
    }

    private IQueryable<CategoriaServicio> BaseQuery(int idNegocio)
    {
        return dbContext.CategoriasServicio
            .Include(categoria => categoria.Negocio)
            .Where(categoria => categoria.IdNegocio == idNegocio);
    }

    private async Task<ServiceResult<CategoriaServicioDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<CategoriaServicioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<CategoriaServicioDto>.Forbidden("No tienes acceso para administrar categorías de servicio de este negocio.");
        }

        return null;
    }

    private async Task<bool> NegocioExistsAsync(int idNegocio, CancellationToken cancellationToken)
    {
        return await dbContext.Negocios.AnyAsync(negocio => negocio.IdNegocio == idNegocio, cancellationToken);
    }

    private async Task<bool> CanManageNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        return await dbContext.NegocioUsuarios.AnyAsync(
            item =>
                item.IdNegocio == idNegocio &&
                item.UserId == currentUser.UserId &&
                item.Activo &&
                (item.RolNegocio.Nombre == OwnerRoleName || item.RolNegocio.Nombre == AdminRoleName),
            cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateNombreAsync(
        int idNegocio,
        string nombre,
        int? currentIdCategoriaServicio,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedName = nombre.Trim();

        var exists = await dbContext.CategoriasServicio.AnyAsync(
            categoria =>
                categoria.IdNegocio == idNegocio &&
                categoria.Nombre == trimmedName &&
                (!currentIdCategoriaServicio.HasValue ||
                    categoria.IdCategoriaServicio != currentIdCategoriaServicio.Value),
            cancellationToken);

        if (exists)
        {
            errors.Add(new ValidationError(nameof(CreateCategoriaServicioRequest.Nombre), "Ya existe una categoría de servicio con ese nombre en este negocio."));
        }

        return errors;
    }

    private static CategoriaServicioDto ToDto(CategoriaServicio categoria)
    {
        return new CategoriaServicioDto(
            categoria.IdCategoriaServicio,
            categoria.IdNegocio,
            categoria.Negocio.Nombre,
            categoria.Nombre,
            categoria.Descripcion,
            categoria.Activo);
    }

    private static object ToAuditSnapshot(CategoriaServicio categoria)
    {
        return new
        {
            categoria.IdCategoriaServicio,
            categoria.IdNegocio,
            Negocio = categoria.Negocio.Nombre,
            categoria.Nombre,
            categoria.Descripcion,
            categoria.Activo
        };
    }
}
