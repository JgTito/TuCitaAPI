using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.CampoReservaOpciones;
using TuCita.Application.Common;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.CampoReservaOpciones;

public sealed class CampoReservaOpcionService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : ICampoReservaOpcionService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";

    public async Task<PagedResult<CampoReservaOpcionDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        CampoReservaOpcionQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CampoReservaExistsAsync(idNegocio, idCampoReserva, cancellationToken) ||
            !await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<CampoReservaOpcionDto>([], query.PageNumber, query.PageSize, 0);
        }

        var opcionesQuery = BaseQuery(idNegocio, idCampoReserva).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            opcionesQuery = opcionesQuery.Where(opcion =>
                opcion.Etiqueta.Contains(search) ||
                opcion.Valor.Contains(search));
        }

        if (query.Activo.HasValue)
        {
            opcionesQuery = opcionesQuery.Where(opcion => opcion.Activo == query.Activo.Value);
        }

        var totalItems = await opcionesQuery.CountAsync(cancellationToken);
        var items = await opcionesQuery
            .OrderBy(opcion => opcion.Orden)
            .ThenBy(opcion => opcion.Etiqueta)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(opcion => new CampoReservaOpcionDto(
                opcion.IdCampoReservaOpcion,
                opcion.IdNegocio,
                opcion.IdCampoReserva,
                opcion.CampoReserva.Etiqueta,
                opcion.Etiqueta,
                opcion.Valor,
                opcion.Orden,
                opcion.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<CampoReservaOpcionDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<CampoReservaOpcionDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idCampoReserva, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var opcion = await BaseQuery(idNegocio, idCampoReserva)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdCampoReservaOpcion == idCampoReservaOpcion, cancellationToken);

        return opcion is null
            ? ServiceResult<CampoReservaOpcionDto>.NotFound("La opción del campo personalizado no existe.")
            : ServiceResult<CampoReservaOpcionDto>.Success(ToDto(opcion));
    }

    public async Task<ServiceResult<CampoReservaOpcionDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        CreateCampoReservaOpcionRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idCampoReserva, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            idCampoReserva,
            request.Valor,
            request.Orden,
            null,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<CampoReservaOpcionDto>.Validation(validationErrors);
        }

        var opcion = new CampoReservaOpcion
        {
            IdNegocio = idNegocio,
            IdCampoReserva = idCampoReserva,
            Etiqueta = request.Etiqueta.Trim(),
            Valor = request.Valor.Trim(),
            Orden = request.Orden,
            Activo = request.Activo
        };

        dbContext.CampoReservaOpciones.Add(opcion);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio, idCampoReserva)
            .AsNoTracking()
            .FirstAsync(item => item.IdCampoReservaOpcion == opcion.IdCampoReservaOpcion, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "FormulariosReserva",
                "CrearOpcionCampo",
                nameof(CampoReservaOpcion),
                created.IdCampoReservaOpcion.ToString(),
                $"Opción creada para el campo {created.CampoReserva.Etiqueta}: {created.Etiqueta}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CampoReservaOpcionDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<CampoReservaOpcionDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        UpdateCampoReservaOpcionRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idCampoReserva, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var opcion = await BaseQuery(idNegocio, idCampoReserva)
            .FirstOrDefaultAsync(item => item.IdCampoReservaOpcion == idCampoReservaOpcion, cancellationToken);

        if (opcion is null)
        {
            return ServiceResult<CampoReservaOpcionDto>.NotFound("La opción del campo personalizado no existe.");
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            idCampoReserva,
            request.Valor,
            request.Orden,
            idCampoReservaOpcion,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<CampoReservaOpcionDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(opcion);

        opcion.Etiqueta = request.Etiqueta.Trim();
        opcion.Valor = request.Valor.Trim();
        opcion.Orden = request.Orden;
        opcion.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio, idCampoReserva)
            .AsNoTracking()
            .FirstAsync(item => item.IdCampoReservaOpcion == idCampoReservaOpcion, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "FormulariosReserva",
                "EditarOpcionCampo",
                nameof(CampoReservaOpcion),
                updated.IdCampoReservaOpcion.ToString(),
                $"Opción editada para el campo {updated.CampoReserva.Etiqueta}: {updated.Etiqueta}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CampoReservaOpcionDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<CampoReservaOpcionDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        bool activo,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idCampoReserva, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var opcion = await BaseQuery(idNegocio, idCampoReserva)
            .FirstOrDefaultAsync(item => item.IdCampoReservaOpcion == idCampoReservaOpcion, cancellationToken);

        if (opcion is null)
        {
            return ServiceResult<CampoReservaOpcionDto>.NotFound("La opción del campo personalizado no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(opcion);
        opcion.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio, idCampoReserva)
            .AsNoTracking()
            .FirstAsync(item => item.IdCampoReservaOpcion == idCampoReservaOpcion, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "FormulariosReserva",
                activo ? "ActivarOpcionCampo" : "DesactivarOpcionCampo",
                nameof(CampoReservaOpcion),
                updated.IdCampoReservaOpcion.ToString(),
                activo
                    ? $"Opción activada para el campo {updated.CampoReserva.Etiqueta}: {updated.Etiqueta}"
                    : $"Opción desactivada para el campo {updated.CampoReserva.Etiqueta}: {updated.Etiqueta}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CampoReservaOpcionDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<CampoReservaOpcionDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, idCampoReserva, idCampoReservaOpcion, activo: false, cancellationToken);
    }

    private IQueryable<CampoReservaOpcion> BaseQuery(int idNegocio, int idCampoReserva)
    {
        return dbContext.CampoReservaOpciones
            .Include(opcion => opcion.CampoReserva)
            .Where(opcion => opcion.IdNegocio == idNegocio && opcion.IdCampoReserva == idCampoReserva);
    }

    private async Task<ServiceResult<CampoReservaOpcionDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<CampoReservaOpcionDto>.NotFound("El negocio no existe.");
        }

        if (!await CampoReservaExistsAsync(idNegocio, idCampoReserva, cancellationToken))
        {
            return ServiceResult<CampoReservaOpcionDto>.NotFound("El campo personalizado de reserva no existe en este negocio.");
        }

        if (!await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<CampoReservaOpcionDto>.Forbidden("No tienes acceso para administrar opciones de campos personalizados.");
        }

        return null;
    }

    private async Task<bool> NegocioExistsAsync(int idNegocio, CancellationToken cancellationToken)
    {
        return await dbContext.Negocios.AnyAsync(negocio => negocio.IdNegocio == idNegocio, cancellationToken);
    }

    private async Task<bool> CampoReservaExistsAsync(
        int idNegocio,
        int idCampoReserva,
        CancellationToken cancellationToken)
    {
        return await dbContext.CamposReserva.AnyAsync(
            campo => campo.IdNegocio == idNegocio && campo.IdCampoReserva == idCampoReserva,
            cancellationToken);
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

    private async Task<List<ValidationError>> ValidateRequestAsync(
        int idNegocio,
        int idCampoReserva,
        string valor,
        int orden,
        int? currentIdCampoReservaOpcion,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedValue = valor.Trim();

        var valorExists = await dbContext.CampoReservaOpciones.AnyAsync(
            opcion =>
                opcion.IdNegocio == idNegocio &&
                opcion.IdCampoReserva == idCampoReserva &&
                opcion.Valor == trimmedValue &&
                (!currentIdCampoReservaOpcion.HasValue ||
                    opcion.IdCampoReservaOpcion != currentIdCampoReservaOpcion.Value),
            cancellationToken);

        if (valorExists)
        {
            errors.Add(new ValidationError(nameof(CreateCampoReservaOpcionRequest.Valor), "Ya existe una opción con ese valor para este campo."));
        }

        if (orden < 0)
        {
            errors.Add(new ValidationError(nameof(CreateCampoReservaOpcionRequest.Orden), "El orden no puede ser negativo."));
        }

        return errors;
    }

    private static CampoReservaOpcionDto ToDto(CampoReservaOpcion opcion)
    {
        return new CampoReservaOpcionDto(
            opcion.IdCampoReservaOpcion,
            opcion.IdNegocio,
            opcion.IdCampoReserva,
            opcion.CampoReserva.Etiqueta,
            opcion.Etiqueta,
            opcion.Valor,
            opcion.Orden,
            opcion.Activo);
    }

    private static object ToAuditSnapshot(CampoReservaOpcion opcion)
    {
        return new
        {
            opcion.IdCampoReservaOpcion,
            opcion.IdNegocio,
            opcion.IdCampoReserva,
            CampoReserva = opcion.CampoReserva?.Etiqueta,
            opcion.Etiqueta,
            opcion.Valor,
            opcion.Orden,
            opcion.Activo
        };
    }
}
