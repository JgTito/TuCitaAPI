using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.TiposPrestador;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.TiposPrestador;

public sealed class TipoPrestadorService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : ITipoPrestadorService
{
    public async Task<PagedResult<TipoPrestadorDto>> GetAllAsync(
        TipoPrestadorQuery query,
        CancellationToken cancellationToken)
    {
        var tiposQuery = dbContext.TiposPrestador.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            tiposQuery = tiposQuery.Where(tipo =>
                tipo.Nombre.Contains(search) ||
                (tipo.Descripcion != null && tipo.Descripcion.Contains(search)));
        }

        if (query.Activo.HasValue)
        {
            tiposQuery = tiposQuery.Where(tipo => tipo.Activo == query.Activo.Value);
        }

        var totalItems = await tiposQuery.CountAsync(cancellationToken);
        var items = await tiposQuery
            .OrderBy(tipo => tipo.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(tipo => new TipoPrestadorDto(
                tipo.IdTipoPrestador,
                tipo.Nombre,
                tipo.Descripcion,
                tipo.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<TipoPrestadorDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<TipoPrestadorDto>> GetByIdAsync(
        int idTipoPrestador,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposPrestador
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdTipoPrestador == idTipoPrestador, cancellationToken);

        return tipo is null
            ? ServiceResult<TipoPrestadorDto>.NotFound("El tipo de prestador no existe.")
            : ServiceResult<TipoPrestadorDto>.Success(ToDto(tipo));
    }

    public async Task<ServiceResult<TipoPrestadorDto>> CreateAsync(
        CurrentUserContext currentUser,
        CreateTipoPrestadorRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateNombreAsync(request.Nombre, null, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<TipoPrestadorDto>.Validation(validationErrors);
        }

        var tipo = new TipoPrestador
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Activo = request.Activo
        };

        dbContext.TiposPrestador.Add(tipo);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Crear",
                nameof(TipoPrestador),
                tipo.IdTipoPrestador.ToString(),
                $"Tipo de prestador creado: {tipo.Nombre}",
                ValoresNuevos: ToAuditSnapshot(tipo)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<TipoPrestadorDto>.Success(ToDto(tipo));
    }

    public async Task<ServiceResult<TipoPrestadorDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idTipoPrestador,
        UpdateTipoPrestadorRequest request,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposPrestador.FirstOrDefaultAsync(
            item => item.IdTipoPrestador == idTipoPrestador,
            cancellationToken);

        if (tipo is null)
        {
            return ServiceResult<TipoPrestadorDto>.NotFound("El tipo de prestador no existe.");
        }

        var validationErrors = await ValidateNombreAsync(request.Nombre, idTipoPrestador, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<TipoPrestadorDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(tipo);

        tipo.Nombre = request.Nombre.Trim();
        tipo.Descripcion = request.Descripcion?.Trim();
        tipo.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Editar",
                nameof(TipoPrestador),
                tipo.IdTipoPrestador.ToString(),
                $"Tipo de prestador editado: {tipo.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(tipo)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<TipoPrestadorDto>.Success(ToDto(tipo));
    }

    public async Task<ServiceResult<TipoPrestadorDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idTipoPrestador,
        bool activo,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposPrestador.FirstOrDefaultAsync(
            item => item.IdTipoPrestador == idTipoPrestador,
            cancellationToken);

        if (tipo is null)
        {
            return ServiceResult<TipoPrestadorDto>.NotFound("El tipo de prestador no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(tipo);
        tipo.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                activo ? "Activar" : "Desactivar",
                nameof(TipoPrestador),
                tipo.IdTipoPrestador.ToString(),
                activo ? $"Tipo de prestador activado: {tipo.Nombre}" : $"Tipo de prestador desactivado: {tipo.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(tipo)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<TipoPrestadorDto>.Success(ToDto(tipo));
    }

    public Task<ServiceResult<TipoPrestadorDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idTipoPrestador,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idTipoPrestador, activo: false, cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateNombreAsync(
        string nombre,
        int? currentIdTipoPrestador,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedName = nombre.Trim();

        var exists = await dbContext.TiposPrestador.AnyAsync(
            tipo => tipo.Nombre == trimmedName && (!currentIdTipoPrestador.HasValue || tipo.IdTipoPrestador != currentIdTipoPrestador.Value),
            cancellationToken);

        if (exists)
        {
            errors.Add(new ValidationError(nameof(CreateTipoPrestadorRequest.Nombre), "Ya existe un tipo de prestador con ese nombre."));
        }

        return errors;
    }

    private static TipoPrestadorDto ToDto(TipoPrestador tipo)
    {
        return new TipoPrestadorDto(
            tipo.IdTipoPrestador,
            tipo.Nombre,
            tipo.Descripcion,
            tipo.Activo);
    }

    private static object ToAuditSnapshot(TipoPrestador tipo)
    {
        return new
        {
            tipo.IdTipoPrestador,
            tipo.Nombre,
            tipo.Descripcion,
            tipo.Activo
        };
    }
}
