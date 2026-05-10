using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Clientes;
using TuCita.Application.Common;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Clientes;

public sealed class ClienteService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IClienteService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const string RecepcionistaRoleName = "Recepcionista";
    private const string ProfesionalRoleName = "Profesional";

    public async Task<PagedResult<ClienteDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ClienteQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanManageClientesAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<ClienteDto>([], query.PageNumber, query.PageSize, 0);
        }

        var clientesQuery = BaseQuery(idNegocio).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            clientesQuery = clientesQuery.Where(cliente =>
                cliente.Nombre.Contains(search) ||
                (cliente.Email != null && cliente.Email.Contains(search)) ||
                (cliente.Telefono != null && cliente.Telefono.Contains(search)) ||
                (cliente.Rut != null && cliente.Rut.Contains(search)) ||
                (cliente.Notas != null && cliente.Notas.Contains(search)) ||
                (cliente.Usuario != null && cliente.Usuario.Email != null && cliente.Usuario.Email.Contains(search)) ||
                (cliente.Usuario != null && cliente.Usuario.UserName != null && cliente.Usuario.UserName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            var userId = query.UserId.Trim();
            clientesQuery = clientesQuery.Where(cliente => cliente.UserId == userId);
        }

        if (query.Activo.HasValue)
        {
            clientesQuery = clientesQuery.Where(cliente => cliente.Activo == query.Activo.Value);
        }

        var totalItems = await clientesQuery.CountAsync(cancellationToken);
        var items = await clientesQuery
            .OrderBy(cliente => cliente.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(cliente => new ClienteDto(
                cliente.IdCliente,
                cliente.IdNegocio,
                cliente.Negocio.Nombre,
                cliente.UserId,
                cliente.Usuario != null ? cliente.Usuario.UserName : null,
                cliente.Usuario != null ? cliente.Usuario.Email : null,
                cliente.Nombre,
                cliente.Telefono,
                cliente.Email,
                cliente.Rut,
                cliente.Notas,
                cliente.Activo,
                cliente.FechaCreacion))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<ClienteDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<IReadOnlyCollection<ClienteSelectDto>> GetSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ClienteSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanReadSelectAsync(currentUser, idNegocio, cancellationToken))
        {
            return [];
        }

        var clientesQuery = BaseQuery(idNegocio).AsNoTracking();
        clientesQuery = ApplySelectFilters(clientesQuery, query);

        return await clientesQuery
            .OrderBy(cliente => cliente.Nombre)
            .Select(cliente => new ClienteSelectDto(
                cliente.IdCliente,
                cliente.Nombre +
                    (cliente.Email != null
                        ? " - " + cliente.Email
                        : cliente.Telefono != null
                            ? " - " + cliente.Telefono
                            : cliente.Rut != null
                                ? " - " + cliente.Rut
                                : string.Empty),
                cliente.Nombre,
                cliente.Email,
                cliente.Telefono,
                cliente.Rut,
                cliente.UserId,
                cliente.Activo))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ServiceResult<ClienteDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCliente,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var cliente = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdCliente == idCliente, cancellationToken);

        return cliente is null
            ? ServiceResult<ClienteDto>.NotFound("El cliente no existe.")
            : ServiceResult<ClienteDto>.Success(ToDto(cliente));
    }

    public async Task<ServiceResult<ClienteDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateClienteRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var email = request.Email?.Trim();
        var userId = await FindUserIdByEmailAsync(email, cancellationToken);
        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            userId,
            null,
            nameof(CreateClienteRequest.Email),
            cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<ClienteDto>.Validation(validationErrors);
        }

        var cliente = new Cliente
        {
            IdNegocio = idNegocio,
            UserId = userId,
            Nombre = request.Nombre.Trim(),
            Telefono = request.Telefono?.Trim(),
            Email = email,
            Rut = request.Rut?.Trim(),
            Notas = request.Notas?.Trim(),
            Activo = request.Activo
        };

        dbContext.Clientes.Add(cliente);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCliente == cliente.IdCliente, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Clientes",
                "Crear",
                nameof(Cliente),
                created.IdCliente.ToString(),
                $"Cliente creado: {created.Nombre}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ClienteDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<ClienteDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCliente,
        UpdateClienteRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var cliente = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdCliente == idCliente, cancellationToken);

        if (cliente is null)
        {
            return ServiceResult<ClienteDto>.NotFound("El cliente no existe.");
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            request.UserId,
            idCliente,
            nameof(UpdateClienteRequest.UserId),
            cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<ClienteDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(cliente);

        cliente.UserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim();
        cliente.Nombre = request.Nombre.Trim();
        cliente.Telefono = request.Telefono?.Trim();
        cliente.Email = request.Email?.Trim();
        cliente.Rut = request.Rut?.Trim();
        cliente.Notas = request.Notas?.Trim();
        cliente.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCliente == idCliente, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Clientes",
                "Editar",
                nameof(Cliente),
                updated.IdCliente.ToString(),
                $"Cliente editado: {updated.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ClienteDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<ClienteDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCliente,
        bool activo,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var cliente = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdCliente == idCliente, cancellationToken);

        if (cliente is null)
        {
            return ServiceResult<ClienteDto>.NotFound("El cliente no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(cliente);
        cliente.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCliente == idCliente, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Clientes",
                activo ? "Activar" : "Desactivar",
                nameof(Cliente),
                updated.IdCliente.ToString(),
                activo ? $"Cliente activado: {updated.Nombre}" : $"Cliente desactivado: {updated.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ClienteDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<ClienteDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCliente,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, idCliente, activo: false, cancellationToken);
    }

    private IQueryable<Cliente> BaseQuery(int idNegocio)
    {
        return dbContext.Clientes
            .Include(cliente => cliente.Negocio)
            .Include(cliente => cliente.Usuario)
            .Where(cliente => cliente.IdNegocio == idNegocio);
    }

    private static IQueryable<Cliente> ApplySelectFilters(
        IQueryable<Cliente> clientesQuery,
        ClienteSelectQuery query)
    {
        if (query.SoloActivos)
        {
            clientesQuery = clientesQuery.Where(cliente => cliente.Activo);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            clientesQuery = clientesQuery.Where(cliente =>
                cliente.Nombre.Contains(search) ||
                (cliente.Email != null && cliente.Email.Contains(search)) ||
                (cliente.Telefono != null && cliente.Telefono.Contains(search)) ||
                (cliente.Rut != null && cliente.Rut.Contains(search)) ||
                (cliente.Usuario != null && cliente.Usuario.Email != null && cliente.Usuario.Email.Contains(search)) ||
                (cliente.Usuario != null && cliente.Usuario.UserName != null && cliente.Usuario.UserName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            var userId = query.UserId.Trim();
            clientesQuery = clientesQuery.Where(cliente => cliente.UserId == userId);
        }

        if (query.TieneUsuario.HasValue)
        {
            clientesQuery = query.TieneUsuario.Value
                ? clientesQuery.Where(cliente => cliente.UserId != null)
                : clientesQuery.Where(cliente => cliente.UserId == null);
        }

        return clientesQuery;
    }

    private async Task<ServiceResult<ClienteDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<ClienteDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageClientesAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<ClienteDto>.Forbidden("No tienes acceso para administrar clientes de este negocio.");
        }

        return null;
    }

    private async Task<bool> NegocioExistsAsync(int idNegocio, CancellationToken cancellationToken)
    {
        return await dbContext.Negocios.AnyAsync(negocio => negocio.IdNegocio == idNegocio, cancellationToken);
    }

    private async Task<bool> CanManageClientesAsync(
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
                    item.RolNegocio.Nombre == RecepcionistaRoleName),
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
        string? userId,
        int? currentIdCliente,
        string userFieldName,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return errors;
        }

        var trimmedUserId = userId.Trim();
        var userExists = await dbContext.Users.AnyAsync(user => user.Id == trimmedUserId, cancellationToken);
        if (!userExists)
        {
            errors.Add(new ValidationError(userFieldName, "El usuario indicado no existe."));
        }

        var relationExists = await dbContext.Clientes.AnyAsync(
            cliente =>
                cliente.IdNegocio == idNegocio &&
                cliente.UserId == trimmedUserId &&
                (!currentIdCliente.HasValue || cliente.IdCliente != currentIdCliente.Value),
            cancellationToken);

        if (relationExists)
        {
            errors.Add(new ValidationError(userFieldName, "El usuario ya está asociado como cliente de este negocio."));
        }

        return errors;
    }

    private async Task<string?> FindUserIdByEmailAsync(string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail = NormalizeEmail(email);

        return await dbContext.Users
            .Where(user => user.NormalizedEmail == normalizedEmail)
            .Select(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static ClienteDto ToDto(Cliente cliente)
    {
        return new ClienteDto(
            cliente.IdCliente,
            cliente.IdNegocio,
            cliente.Negocio.Nombre,
            cliente.UserId,
            cliente.Usuario?.UserName,
            cliente.Usuario?.Email,
            cliente.Nombre,
            cliente.Telefono,
            cliente.Email,
            cliente.Rut,
            cliente.Notas,
            cliente.Activo,
            cliente.FechaCreacion);
    }

    private static object ToAuditSnapshot(Cliente cliente)
    {
        return new
        {
            cliente.IdCliente,
            cliente.IdNegocio,
            Negocio = cliente.Negocio?.Nombre,
            cliente.UserId,
            Usuario = cliente.Usuario?.Email ?? cliente.Usuario?.UserName,
            cliente.Nombre,
            cliente.Telefono,
            cliente.Email,
            cliente.Rut,
            cliente.Notas,
            cliente.Activo,
            cliente.FechaCreacion
        };
    }
}
