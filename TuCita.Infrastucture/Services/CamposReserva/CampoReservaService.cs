using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.CamposReserva;
using TuCita.Application.Common;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.CamposReserva;

public sealed class CampoReservaService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : ICampoReservaService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const string RecepcionistaRoleName = "Recepcionista";
    private const string ProfesionalRoleName = "Profesional";

    public async Task<PagedResult<CampoReservaDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CampoReservaQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<CampoReservaDto>([], query.PageNumber, query.PageSize, 0);
        }

        var camposQuery = BaseQuery(idNegocio).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            camposQuery = camposQuery.Where(campo =>
                campo.NombreInterno.Contains(search) ||
                campo.Etiqueta.Contains(search) ||
                (campo.Placeholder != null && campo.Placeholder.Contains(search)) ||
                (campo.TextoAyuda != null && campo.TextoAyuda.Contains(search)) ||
                campo.TipoCampo.Nombre.Contains(search));
        }

        if (query.IdTipoCampo.HasValue)
        {
            camposQuery = camposQuery.Where(campo => campo.IdTipoCampo == query.IdTipoCampo.Value);
        }

        if (query.IdServicio.HasValue)
        {
            camposQuery = camposQuery.Where(campo => campo.IdServicio == query.IdServicio.Value);
        }

        if (query.SoloGlobales.HasValue)
        {
            camposQuery = query.SoloGlobales.Value
                ? camposQuery.Where(campo => !campo.IdServicio.HasValue)
                : camposQuery.Where(campo => campo.IdServicio.HasValue);
        }

        if (query.Obligatorio.HasValue)
        {
            camposQuery = camposQuery.Where(campo => campo.Obligatorio == query.Obligatorio.Value);
        }

        if (query.Activo.HasValue)
        {
            camposQuery = camposQuery.Where(campo => campo.Activo == query.Activo.Value);
        }

        var totalItems = await camposQuery.CountAsync(cancellationToken);
        var items = await camposQuery
            .OrderBy(campo => campo.Orden)
            .ThenBy(campo => campo.Etiqueta)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(campo => new CampoReservaDto(
                campo.IdCampoReserva,
                campo.IdNegocio,
                campo.Negocio.Nombre,
                campo.IdServicio,
                campo.Servicio == null ? null : campo.Servicio.Nombre,
                campo.IdTipoCampo,
                campo.TipoCampo.Nombre,
                campo.NombreInterno,
                campo.Etiqueta,
                campo.Placeholder,
                campo.TextoAyuda,
                campo.Obligatorio,
                campo.Orden,
                campo.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<CampoReservaDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<IReadOnlyCollection<CampoReservaSelectDto>> GetSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CampoReservaSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanReadSelectAsync(currentUser, idNegocio, cancellationToken))
        {
            return [];
        }

        var camposQuery = BaseQuery(idNegocio).AsNoTracking();
        camposQuery = ApplySelectFilters(camposQuery, query);

        return await camposQuery
            .OrderBy(campo => campo.Orden)
            .ThenBy(campo => campo.Etiqueta)
            .Select(campo => new CampoReservaSelectDto(
                campo.IdCampoReserva,
                campo.IdServicio.HasValue
                    ? campo.Etiqueta + " - " + campo.Servicio!.Nombre
                    : campo.Etiqueta,
                campo.NombreInterno,
                campo.Etiqueta,
                campo.IdTipoCampo,
                campo.TipoCampo.Nombre,
                campo.IdServicio,
                campo.Servicio == null ? null : campo.Servicio.Nombre,
                campo.Obligatorio,
                campo.Orden,
                campo.Activo))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ServiceResult<CampoReservaDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var campo = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdCampoReserva == idCampoReserva, cancellationToken);

        return campo is null
            ? ServiceResult<CampoReservaDto>.NotFound("El campo personalizado de reserva no existe.")
            : ServiceResult<CampoReservaDto>.Success(ToDto(campo));
    }

    public async Task<ServiceResult<CampoReservaDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateCampoReservaRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            request.IdServicio,
            request.IdTipoCampo,
            request.NombreInterno,
            request.Orden,
            null,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<CampoReservaDto>.Validation(validationErrors);
        }

        var campo = new CampoReserva
        {
            IdNegocio = idNegocio,
            IdServicio = request.IdServicio,
            IdTipoCampo = request.IdTipoCampo,
            NombreInterno = request.NombreInterno.Trim(),
            Etiqueta = request.Etiqueta.Trim(),
            Placeholder = request.Placeholder?.Trim(),
            TextoAyuda = request.TextoAyuda?.Trim(),
            Obligatorio = request.Obligatorio,
            Orden = request.Orden,
            Activo = request.Activo
        };

        dbContext.CamposReserva.Add(campo);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCampoReserva == campo.IdCampoReserva, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "FormulariosReserva",
                "CrearCampo",
                nameof(CampoReserva),
                created.IdCampoReserva.ToString(),
                $"Campo personalizado creado: {created.Etiqueta}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CampoReservaDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<CampoReservaDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        UpdateCampoReservaRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var campo = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdCampoReserva == idCampoReserva, cancellationToken);

        if (campo is null)
        {
            return ServiceResult<CampoReservaDto>.NotFound("El campo personalizado de reserva no existe.");
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            request.IdServicio,
            request.IdTipoCampo,
            request.NombreInterno,
            request.Orden,
            idCampoReserva,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<CampoReservaDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(campo);

        campo.IdServicio = request.IdServicio;
        campo.IdTipoCampo = request.IdTipoCampo;
        campo.NombreInterno = request.NombreInterno.Trim();
        campo.Etiqueta = request.Etiqueta.Trim();
        campo.Placeholder = request.Placeholder?.Trim();
        campo.TextoAyuda = request.TextoAyuda?.Trim();
        campo.Obligatorio = request.Obligatorio;
        campo.Orden = request.Orden;
        campo.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCampoReserva == idCampoReserva, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "FormulariosReserva",
                "EditarCampo",
                nameof(CampoReserva),
                updated.IdCampoReserva.ToString(),
                $"Campo personalizado editado: {updated.Etiqueta}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CampoReservaDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<CampoReservaDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        bool activo,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var campo = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdCampoReserva == idCampoReserva, cancellationToken);

        if (campo is null)
        {
            return ServiceResult<CampoReservaDto>.NotFound("El campo personalizado de reserva no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(campo);
        campo.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCampoReserva == idCampoReserva, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "FormulariosReserva",
                activo ? "ActivarCampo" : "DesactivarCampo",
                nameof(CampoReserva),
                updated.IdCampoReserva.ToString(),
                activo ? $"Campo personalizado activado: {updated.Etiqueta}" : $"Campo personalizado desactivado: {updated.Etiqueta}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CampoReservaDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<CampoReservaDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, idCampoReserva, activo: false, cancellationToken);
    }

    private IQueryable<CampoReserva> BaseQuery(int idNegocio)
    {
        return dbContext.CamposReserva
            .Include(campo => campo.Negocio)
            .Include(campo => campo.Servicio)
            .Include(campo => campo.TipoCampo)
            .Where(campo => campo.IdNegocio == idNegocio);
    }

    private static IQueryable<CampoReserva> ApplySelectFilters(
        IQueryable<CampoReserva> camposQuery,
        CampoReservaSelectQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            camposQuery = camposQuery.Where(campo =>
                campo.NombreInterno.Contains(search) ||
                campo.Etiqueta.Contains(search) ||
                campo.TipoCampo.Nombre.Contains(search) ||
                (campo.Servicio != null && campo.Servicio.Nombre.Contains(search)));
        }

        if (query.IdTipoCampo.HasValue)
        {
            camposQuery = camposQuery.Where(campo => campo.IdTipoCampo == query.IdTipoCampo.Value);
        }

        if (query.IdServicio.HasValue)
        {
            camposQuery = camposQuery.Where(campo =>
                !campo.IdServicio.HasValue ||
                campo.IdServicio == query.IdServicio.Value);
        }

        if (query.SoloGlobales.HasValue)
        {
            camposQuery = query.SoloGlobales.Value
                ? camposQuery.Where(campo => !campo.IdServicio.HasValue)
                : camposQuery.Where(campo => campo.IdServicio.HasValue);
        }

        if (query.Obligatorio.HasValue)
        {
            camposQuery = camposQuery.Where(campo => campo.Obligatorio == query.Obligatorio.Value);
        }

        if (query.SoloActivos)
        {
            camposQuery = camposQuery.Where(campo => campo.Activo);
        }

        return camposQuery;
    }

    private async Task<ServiceResult<CampoReservaDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<CampoReservaDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<CampoReservaDto>.Forbidden("No tienes acceso para administrar campos personalizados de este negocio.");
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

    private async Task<bool> CanReadSelectAsync(
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
                (item.RolNegocio.Nombre == OwnerRoleName ||
                    item.RolNegocio.Nombre == AdminRoleName ||
                    item.RolNegocio.Nombre == RecepcionistaRoleName ||
                    item.RolNegocio.Nombre == ProfesionalRoleName),
            cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateRequestAsync(
        int idNegocio,
        int? idServicio,
        int idTipoCampo,
        string nombreInterno,
        int orden,
        int? currentIdCampoReserva,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedName = nombreInterno.Trim();

        var tipoExists = await dbContext.TiposCampo.AnyAsync(
            tipo => tipo.IdTipoCampo == idTipoCampo && tipo.Activo,
            cancellationToken);

        if (!tipoExists)
        {
            errors.Add(new ValidationError(nameof(CreateCampoReservaRequest.IdTipoCampo), "El tipo de campo indicado no existe o no está activo."));
        }

        if (idServicio.HasValue)
        {
            var servicioExists = await dbContext.Servicios.AnyAsync(
                servicio => servicio.IdNegocio == idNegocio && servicio.IdServicio == idServicio.Value,
                cancellationToken);

            if (!servicioExists)
            {
                errors.Add(new ValidationError(nameof(CreateCampoReservaRequest.IdServicio), "El servicio indicado no existe para este negocio."));
            }
        }

        var nombreExists = await dbContext.CamposReserva.AnyAsync(
            campo =>
                campo.IdNegocio == idNegocio &&
                campo.NombreInterno == trimmedName &&
                (!campo.IdServicio.HasValue ||
                    !idServicio.HasValue ||
                    campo.IdServicio == idServicio.Value) &&
                (!currentIdCampoReserva.HasValue ||
                    campo.IdCampoReserva != currentIdCampoReserva.Value),
            cancellationToken);

        if (nombreExists)
        {
            errors.Add(new ValidationError(nameof(CreateCampoReservaRequest.NombreInterno), "Ya existe un campo personalizado con ese nombre interno en este negocio."));
        }

        if (orden < 0)
        {
            errors.Add(new ValidationError(nameof(CreateCampoReservaRequest.Orden), "El orden no puede ser negativo."));
        }

        return errors;
    }

    private static CampoReservaDto ToDto(CampoReserva campo)
    {
        return new CampoReservaDto(
            campo.IdCampoReserva,
            campo.IdNegocio,
            campo.Negocio.Nombre,
            campo.IdServicio,
            campo.Servicio?.Nombre,
            campo.IdTipoCampo,
            campo.TipoCampo.Nombre,
            campo.NombreInterno,
            campo.Etiqueta,
            campo.Placeholder,
            campo.TextoAyuda,
            campo.Obligatorio,
            campo.Orden,
            campo.Activo);
    }

    private static object ToAuditSnapshot(CampoReserva campo)
    {
        return new
        {
            campo.IdCampoReserva,
            campo.IdNegocio,
            Negocio = campo.Negocio?.Nombre,
            campo.IdServicio,
            Servicio = campo.Servicio?.Nombre,
            campo.IdTipoCampo,
            TipoCampo = campo.TipoCampo?.Nombre,
            campo.NombreInterno,
            campo.Etiqueta,
            campo.Placeholder,
            campo.TextoAyuda,
            campo.Obligatorio,
            campo.Orden,
            campo.Activo
        };
    }
}
