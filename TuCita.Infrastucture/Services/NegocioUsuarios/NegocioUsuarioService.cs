using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.NegocioUsuarios;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.NegocioUsuarios;

public sealed class NegocioUsuarioService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : INegocioUsuarioService
{
    private static readonly string[] ManagerRoles = ["Owner", "Admin"];
    private const string OwnerRoleName = "Owner";

    public async Task<PagedResult<NegocioUsuarioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        NegocioUsuarioQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<NegocioUsuarioDto>([], query.PageNumber, query.PageSize, 0);
        }

        var usuariosQuery = BaseQuery(idNegocio).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            usuariosQuery = usuariosQuery.Where(item =>
                (item.Usuario.UserName != null && item.Usuario.UserName.Contains(search)) ||
                (item.Usuario.Email != null && item.Usuario.Email.Contains(search)) ||
                item.RolNegocio.Nombre.Contains(search));
        }

        if (query.IdRolNegocio.HasValue)
        {
            usuariosQuery = usuariosQuery.Where(item => item.IdRolNegocio == query.IdRolNegocio.Value);
        }

        if (query.Activo.HasValue)
        {
            usuariosQuery = usuariosQuery.Where(item => item.Activo == query.Activo.Value);
        }

        var totalItems = await usuariosQuery.CountAsync(cancellationToken);
        var items = await usuariosQuery
            .OrderBy(item => item.Usuario.Email)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new NegocioUsuarioDto(
                item.IdNegocioUsuario,
                item.IdNegocio,
                item.Negocio.Nombre,
                item.UserId,
                item.Usuario.UserName,
                item.Usuario.Email,
                item.Usuario.PhoneNumber,
                item.IdRolNegocio,
                item.RolNegocio.Nombre,
                item.Activo,
                item.FechaCreacion))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<NegocioUsuarioDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<IReadOnlyCollection<NegocioUsuarioSelectDto>> GetSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        NegocioUsuarioSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await IsUserAssociatedToNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return [];
        }

        var usuariosQuery = BaseQuery(idNegocio).AsNoTracking();

        if (query.SoloActivos)
        {
            usuariosQuery = usuariosQuery.Where(item => item.Activo);
        }

        if (query.IdRolNegocio.HasValue)
        {
            usuariosQuery = usuariosQuery.Where(item => item.IdRolNegocio == query.IdRolNegocio.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            usuariosQuery = usuariosQuery.Where(item =>
                (item.Usuario.UserName != null && item.Usuario.UserName.Contains(search)) ||
                (item.Usuario.Email != null && item.Usuario.Email.Contains(search)) ||
                item.RolNegocio.Nombre.Contains(search));
        }

        return await usuariosQuery
            .OrderBy(item => item.Usuario.Email ?? item.Usuario.UserName)
            .Select(item => new NegocioUsuarioSelectDto(
                item.IdNegocioUsuario,
                item.UserId,
                item.Usuario.Email ?? item.Usuario.UserName ?? item.UserId,
                item.Usuario.Email,
                item.Usuario.UserName,
                item.IdRolNegocio,
                item.RolNegocio.Nombre,
                item.Activo))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ServiceResult<NegocioUsuarioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idNegocioUsuario,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var usuario = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocioUsuario == idNegocioUsuario, cancellationToken);

        return usuario is null
            ? ServiceResult<NegocioUsuarioDto>.NotFound("El usuario del negocio no existe.")
            : ServiceResult<NegocioUsuarioDto>.Success(ToDto(usuario));
    }

    public async Task<ServiceResult<NegocioUsuarioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateNegocioUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var validationErrors = await ValidateCreateAsync(idNegocio, request, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<NegocioUsuarioDto>.Validation(validationErrors);
        }

        var negocioUsuario = new NegocioUsuario
        {
            IdNegocio = idNegocio,
            UserId = request.UserId,
            IdRolNegocio = request.IdRolNegocio,
            Activo = request.Activo
        };

        dbContext.NegocioUsuarios.Add(negocioUsuario);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdNegocioUsuario == negocioUsuario.IdNegocioUsuario, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "UsuariosNegocio",
                "Crear",
                nameof(NegocioUsuario),
                created.IdNegocioUsuario.ToString(),
                $"Usuario asociado al negocio: {created.Usuario.Email ?? created.Usuario.UserName ?? created.UserId}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<NegocioUsuarioDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<NegocioUsuarioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idNegocioUsuario,
        UpdateNegocioUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var negocioUsuario = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdNegocioUsuario == idNegocioUsuario, cancellationToken);

        if (negocioUsuario is null)
        {
            return ServiceResult<NegocioUsuarioDto>.NotFound("El usuario del negocio no existe.");
        }

        var validationErrors = await ValidateUpdateAsync(idNegocio, negocioUsuario, request, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<NegocioUsuarioDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(negocioUsuario);

        negocioUsuario.IdRolNegocio = request.IdRolNegocio;
        negocioUsuario.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdNegocioUsuario == idNegocioUsuario, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "UsuariosNegocio",
                "Editar",
                nameof(NegocioUsuario),
                idNegocioUsuario.ToString(),
                $"Usuario del negocio editado: {updated.Usuario.Email ?? updated.Usuario.UserName ?? updated.UserId}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<NegocioUsuarioDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<NegocioUsuarioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idNegocioUsuario,
        bool activo,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var negocioUsuario = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdNegocioUsuario == idNegocioUsuario, cancellationToken);

        if (negocioUsuario is null)
        {
            return ServiceResult<NegocioUsuarioDto>.NotFound("El usuario del negocio no existe.");
        }

        if (!activo && await IsLastActiveOwnerAsync(idNegocio, negocioUsuario, cancellationToken))
        {
            return ServiceResult<NegocioUsuarioDto>.Validation([
                new ValidationError(nameof(UpdateNegocioUsuarioRequest.Activo), "No puedes desactivar el último Owner activo del negocio.")
            ]);
        }

        var previousSnapshot = ToAuditSnapshot(negocioUsuario);

        negocioUsuario.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdNegocioUsuario == idNegocioUsuario, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "UsuariosNegocio",
                activo ? "Activar" : "Desactivar",
                nameof(NegocioUsuario),
                idNegocioUsuario.ToString(),
                activo
                    ? $"Usuario del negocio activado: {updated.Usuario.Email ?? updated.Usuario.UserName ?? updated.UserId}"
                    : $"Usuario del negocio desactivado: {updated.Usuario.Email ?? updated.Usuario.UserName ?? updated.UserId}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<NegocioUsuarioDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<NegocioUsuarioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idNegocioUsuario,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, idNegocioUsuario, activo: false, cancellationToken);
    }

    private IQueryable<NegocioUsuario> BaseQuery(int idNegocio)
    {
        return dbContext.NegocioUsuarios
            .Include(item => item.Negocio)
            .Include(item => item.Usuario)
            .Include(item => item.RolNegocio)
            .Where(item => item.IdNegocio == idNegocio);
    }

    private async Task<ServiceResult<NegocioUsuarioDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<NegocioUsuarioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<NegocioUsuarioDto>.Forbidden("No tienes acceso para administrar usuarios de este negocio.");
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
                (item.RolNegocio.Nombre == ManagerRoles[0] || item.RolNegocio.Nombre == ManagerRoles[1]),
            cancellationToken);
    }

    private async Task<bool> IsUserAssociatedToNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return false;
        }

        return await dbContext.NegocioUsuarios.AnyAsync(
            item =>
                item.IdNegocio == idNegocio &&
                item.UserId == currentUser.UserId &&
                item.Activo,
            cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateCreateAsync(
        int idNegocio,
        CreateNegocioUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        var userExists = await dbContext.Users.AnyAsync(user => user.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            errors.Add(new ValidationError(nameof(CreateNegocioUsuarioRequest.UserId), "El usuario indicado no existe."));
        }

        await AddRoleValidationAsync(errors, request.IdRolNegocio, cancellationToken);

        var relationExists = await dbContext.NegocioUsuarios.AnyAsync(
            item => item.IdNegocio == idNegocio && item.UserId == request.UserId,
            cancellationToken);

        if (relationExists)
        {
            errors.Add(new ValidationError(nameof(CreateNegocioUsuarioRequest.UserId), "El usuario ya está asociado a este negocio."));
        }

        return errors;
    }

    private async Task<List<ValidationError>> ValidateUpdateAsync(
        int idNegocio,
        NegocioUsuario negocioUsuario,
        UpdateNegocioUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        await AddRoleValidationAsync(errors, request.IdRolNegocio, cancellationToken);

        if (await IsLastActiveOwnerAsync(idNegocio, negocioUsuario, cancellationToken))
        {
            var ownerRole = await dbContext.RolesNegocio
                .AsNoTracking()
                .FirstOrDefaultAsync(role => role.IdRolNegocio == request.IdRolNegocio, cancellationToken);

            if (!request.Activo)
            {
                errors.Add(new ValidationError(nameof(UpdateNegocioUsuarioRequest.Activo), "No puedes desactivar el último Owner activo del negocio."));
            }

            if (ownerRole is null || ownerRole.Nombre != OwnerRoleName)
            {
                errors.Add(new ValidationError(nameof(UpdateNegocioUsuarioRequest.IdRolNegocio), "No puedes cambiar el rol del último Owner activo del negocio."));
            }
        }

        return errors;
    }

    private async Task AddRoleValidationAsync(
        List<ValidationError> errors,
        int idRolNegocio,
        CancellationToken cancellationToken)
    {
        var roleExists = await dbContext.RolesNegocio.AnyAsync(
            role => role.IdRolNegocio == idRolNegocio && role.Activo,
            cancellationToken);

        if (!roleExists)
        {
            errors.Add(new ValidationError(nameof(CreateNegocioUsuarioRequest.IdRolNegocio), "El rol de negocio indicado no existe o no está activo."));
        }
    }

    private async Task<bool> IsLastActiveOwnerAsync(
        int idNegocio,
        NegocioUsuario negocioUsuario,
        CancellationToken cancellationToken)
    {
        if (!negocioUsuario.Activo || negocioUsuario.RolNegocio.Nombre != OwnerRoleName)
        {
            return false;
        }

        var activeOwners = await dbContext.NegocioUsuarios.CountAsync(
            item =>
                item.IdNegocio == idNegocio &&
                item.Activo &&
                item.RolNegocio.Nombre == OwnerRoleName,
            cancellationToken);

        return activeOwners <= 1;
    }

    private static NegocioUsuarioDto ToDto(NegocioUsuario item)
    {
        return new NegocioUsuarioDto(
            item.IdNegocioUsuario,
            item.IdNegocio,
            item.Negocio.Nombre,
            item.UserId,
            item.Usuario.UserName,
            item.Usuario.Email,
            item.Usuario.PhoneNumber,
            item.IdRolNegocio,
            item.RolNegocio.Nombre,
            item.Activo,
            item.FechaCreacion);
    }

    private static object ToAuditSnapshot(NegocioUsuario item)
    {
        return new
        {
            item.IdNegocioUsuario,
            item.IdNegocio,
            Negocio = item.Negocio.Nombre,
            item.UserId,
            Usuario = item.Usuario.Email ?? item.Usuario.UserName,
            item.Usuario.Email,
            item.Usuario.PhoneNumber,
            item.IdRolNegocio,
            Rol = item.RolNegocio.Nombre,
            item.Activo,
            item.FechaCreacion
        };
    }
}
