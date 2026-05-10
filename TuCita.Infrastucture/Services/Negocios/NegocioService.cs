using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.Negocios;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Negocios;

public sealed class NegocioService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : INegocioService
{
    private const string OwnerRoleName = "Owner";

    public async Task<PagedResult<NegocioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        NegocioQuery query,
        CancellationToken cancellationToken)
    {
        var negociosQuery = ApplyUserScope(
            dbContext.Negocios
                .AsNoTracking()
                .Include(negocio => negocio.Rubro),
            currentUser);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            negociosQuery = negociosQuery.Where(negocio =>
                negocio.Nombre.Contains(search) ||
                negocio.Slug.Contains(search) ||
                (negocio.Email != null && negocio.Email.Contains(search)) ||
                (negocio.Telefono != null && negocio.Telefono.Contains(search)));
        }

        if (query.IdRubro.HasValue)
        {
            negociosQuery = negociosQuery.Where(negocio => negocio.IdRubro == query.IdRubro.Value);
        }

        if (query.Activo.HasValue)
        {
            negociosQuery = negociosQuery.Where(negocio => negocio.Activo == query.Activo.Value);
        }

        var totalItems = await negociosQuery.CountAsync(cancellationToken);
        var items = await negociosQuery
            .OrderBy(negocio => negocio.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(negocio => new NegocioDto(
                negocio.IdNegocio,
                negocio.IdRubro,
                negocio.Rubro.Nombre,
                negocio.Nombre,
                negocio.Slug,
                negocio.Descripcion,
                negocio.LogoUrl,
                negocio.Direccion,
                negocio.Telefono,
                negocio.Email,
                negocio.Activo,
                negocio.FechaCreacion))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<NegocioDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<NegocioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var negocio = await FindByIdQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio, cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<NegocioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanAccessNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<NegocioDto>.Forbidden("No tienes acceso a este negocio.");
        }

        return ServiceResult<NegocioDto>.Success(ToDto(negocio));
    }

    public async Task<ServiceResult<NegocioDto>> CreateAsync(
        CurrentUserContext currentUser,
        CreateNegocioRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<NegocioDto>.Forbidden("Usuario no autenticado.");
        }

        var validationErrors = await ValidateRequestAsync(request.IdRubro, request.Slug, null, cancellationToken);
        var ownerRole = await dbContext.RolesNegocio.FirstOrDefaultAsync(
            role => role.Nombre == OwnerRoleName && role.Activo,
            cancellationToken);

        if (ownerRole is null)
        {
            validationErrors.Add(new ValidationError(string.Empty, "No existe un rol de negocio Owner activo para asociar el negocio."));
        }

        if (validationErrors.Count > 0)
        {
            return ServiceResult<NegocioDto>.Validation(validationErrors);
        }

        var negocio = new Negocio
        {
            IdRubro = request.IdRubro,
            Nombre = request.Nombre.Trim(),
            Slug = request.Slug.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            LogoUrl = request.LogoUrl?.Trim(),
            Direccion = request.Direccion?.Trim(),
            Telefono = request.Telefono?.Trim(),
            Email = request.Email?.Trim(),
            Activo = request.Activo
        };

        dbContext.Negocios.Add(negocio);
        dbContext.ReglasReserva.Add(new ReglaReserva
        {
            Negocio = negocio
        });

        if (ownerRole is not null)
        {
            dbContext.NegocioUsuarios.Add(new NegocioUsuario
            {
                Negocio = negocio,
                UserId = currentUser.UserId,
                IdRolNegocio = ownerRole.IdRolNegocio,
                Activo = true
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await FindByIdQuery()
            .AsNoTracking()
            .FirstAsync(item => item.IdNegocio == negocio.IdNegocio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                created.IdNegocio,
                "Negocios",
                "Crear",
                nameof(Negocio),
                created.IdNegocio.ToString(),
                $"Negocio creado: {created.Nombre}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);

        var ownerRelation = await dbContext.NegocioUsuarios
            .AsNoTracking()
            .Include(relacion => relacion.Negocio)
            .Include(relacion => relacion.RolNegocio)
            .Include(relacion => relacion.Usuario)
            .FirstOrDefaultAsync(
                relacion => relacion.IdNegocio == created.IdNegocio && relacion.UserId == currentUser.UserId,
                cancellationToken);

        if (ownerRelation is not null)
        {
            await auditoriaService.RegistrarAsync(
                currentUser,
                new AuditoriaRegistro(
                    created.IdNegocio,
                    "UsuariosNegocio",
                    "AsignarOwnerInicial",
                    nameof(NegocioUsuario),
                    ownerRelation.IdNegocioUsuario.ToString(),
                    $"Usuario asociado como Owner inicial al negocio {created.Nombre}.",
                    ValoresNuevos: ToNegocioUsuarioAuditSnapshot(ownerRelation)),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<NegocioDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<NegocioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        UpdateNegocioRequest request,
        CancellationToken cancellationToken)
    {
        var negocio = await FindByIdQuery()
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio, cancellationToken);
        if (negocio is null)
        {
            return ServiceResult<NegocioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanAccessNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<NegocioDto>.Forbidden("No tienes acceso para modificar este negocio.");
        }

        var validationErrors = await ValidateRequestAsync(request.IdRubro, request.Slug, idNegocio, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<NegocioDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(negocio);

        negocio.IdRubro = request.IdRubro;
        negocio.Nombre = request.Nombre.Trim();
        negocio.Slug = request.Slug.Trim();
        negocio.Descripcion = request.Descripcion?.Trim();
        if (!string.IsNullOrWhiteSpace(request.LogoUrl))
        {
            negocio.LogoUrl = request.LogoUrl.Trim();
        }
        negocio.Direccion = request.Direccion?.Trim();
        negocio.Telefono = request.Telefono?.Trim();
        negocio.Email = request.Email?.Trim();
        negocio.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await FindByIdQuery()
            .AsNoTracking()
            .FirstAsync(item => item.IdNegocio == idNegocio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                updated.IdNegocio,
                "Negocios",
                "Editar",
                nameof(Negocio),
                updated.IdNegocio.ToString(),
                $"Negocio editado: {updated.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<NegocioDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<NegocioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        bool activo,
        CancellationToken cancellationToken)
    {
        var negocio = await FindByIdQuery()
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio, cancellationToken);
        if (negocio is null)
        {
            return ServiceResult<NegocioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanAccessNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<NegocioDto>.Forbidden("No tienes acceso para cambiar el estado de este negocio.");
        }

        var previousSnapshot = ToAuditSnapshot(negocio);
        negocio.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await FindByIdQuery()
            .AsNoTracking()
            .FirstAsync(item => item.IdNegocio == idNegocio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                updated.IdNegocio,
                "Negocios",
                activo ? "Activar" : "Desactivar",
                nameof(Negocio),
                updated.IdNegocio.ToString(),
                activo ? $"Negocio activado: {updated.Nombre}" : $"Negocio desactivado: {updated.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<NegocioDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<NegocioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, activo: false, cancellationToken);
    }

    private IQueryable<Negocio> FindByIdQuery()
    {
        return dbContext.Negocios.Include(negocio => negocio.Rubro);
    }

    private IQueryable<Negocio> ApplyUserScope(IQueryable<Negocio> query, CurrentUserContext currentUser)
    {
        if (currentUser.IsSuperAdmin)
        {
            return query;
        }

        return query.Where(negocio => negocio.NegocioUsuarios.Any(
            negocioUsuario => negocioUsuario.UserId == currentUser.UserId && negocioUsuario.Activo));
    }

    private async Task<bool> CanAccessNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        return await dbContext.NegocioUsuarios.AnyAsync(
            negocioUsuario =>
                negocioUsuario.IdNegocio == idNegocio &&
                negocioUsuario.UserId == currentUser.UserId &&
                negocioUsuario.Activo,
            cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateRequestAsync(
        int idRubro,
        string slug,
        int? currentIdNegocio,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedSlug = slug.Trim();

        var rubroExists = await dbContext.Rubros.AnyAsync(rubro => rubro.IdRubro == idRubro && rubro.Activo, cancellationToken);
        if (!rubroExists)
        {
            errors.Add(new ValidationError(nameof(CreateNegocioRequest.IdRubro), "El rubro indicado no existe o no está activo."));
        }

        var slugExists = await dbContext.Negocios.AnyAsync(
            negocio => negocio.Slug == trimmedSlug && (!currentIdNegocio.HasValue || negocio.IdNegocio != currentIdNegocio.Value),
            cancellationToken);

        if (slugExists)
        {
            errors.Add(new ValidationError(nameof(CreateNegocioRequest.Slug), "Ya existe un negocio con ese slug."));
        }

        return errors;
    }

    private static NegocioDto ToDto(Negocio negocio)
    {
        return new NegocioDto(
            negocio.IdNegocio,
            negocio.IdRubro,
            negocio.Rubro.Nombre,
            negocio.Nombre,
            negocio.Slug,
            negocio.Descripcion,
            negocio.LogoUrl,
            negocio.Direccion,
            negocio.Telefono,
            negocio.Email,
            negocio.Activo,
            negocio.FechaCreacion);
    }

    private static object ToAuditSnapshot(Negocio negocio)
    {
        return new
        {
            negocio.IdNegocio,
            negocio.IdRubro,
            Rubro = negocio.Rubro.Nombre,
            negocio.Nombre,
            negocio.Slug,
            negocio.Descripcion,
            negocio.LogoUrl,
            negocio.Direccion,
            negocio.Telefono,
            negocio.Email,
            negocio.Activo,
            negocio.FechaCreacion
        };
    }

    private static object ToNegocioUsuarioAuditSnapshot(NegocioUsuario relacion)
    {
        return new
        {
            relacion.IdNegocioUsuario,
            relacion.IdNegocio,
            Negocio = relacion.Negocio.Nombre,
            relacion.UserId,
            Usuario = relacion.Usuario.Email ?? relacion.Usuario.UserName,
            relacion.IdRolNegocio,
            Rol = relacion.RolNegocio.Nombre,
            relacion.Activo,
            relacion.FechaCreacion
        };
    }
}
