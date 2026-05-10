using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Clientes;
using TuCita.Application.Common;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Clientes;

public sealed class ClienteResolverService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IClienteResolverService
{
    public async Task<ServiceResult<ClienteReservaDto>> ResolveForReservaPublicaAsync(
        CurrentUserContext currentUser,
        ResolveClienteReservaRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = DataAnnotationsValidator.Validate(request).ToList();
        if (validationErrors.Count > 0)
        {
            return ServiceResult<ClienteReservaDto>.Validation(validationErrors);
        }

        var negocioExists = await dbContext.Negocios.AnyAsync(
            negocio => negocio.IdNegocio == request.IdNegocio && negocio.Activo,
            cancellationToken);

        if (!negocioExists)
        {
            return ServiceResult<ClienteReservaDto>.NotFound("El negocio no existe o no está activo.");
        }

        var authenticatedUser = await GetAuthenticatedUserDataAsync(currentUser, cancellationToken);
        if (currentUser.IsAuthenticated && authenticatedUser is null)
        {
            return ServiceResult<ClienteReservaDto>.Forbidden("El usuario autenticado no existe.");
        }

        var email = TrimToNull(request.Email);
        if (authenticatedUser is not null)
        {
            var emailValidation = ValidateAuthenticatedEmail(email, authenticatedUser);
            if (emailValidation is not null)
            {
                return ServiceResult<ClienteReservaDto>.Validation([emailValidation]);
            }
        }

        var userId = authenticatedUser?.UserId;
        var emailToStore = email ?? TrimToNull(authenticatedUser?.Email);
        var cliente = await FindClienteAsync(request.IdNegocio, userId, emailToStore, cancellationToken);
        var isNew = cliente is null;
        var previousSnapshot = cliente is null ? null : ToAuditSnapshot(cliente);

        if (cliente is null)
        {
            cliente = new Cliente
            {
                IdNegocio = request.IdNegocio
            };

            dbContext.Clientes.Add(cliente);
        }

        ApplyReservaData(cliente, userId, request.Nombre, emailToStore, request.Telefono, request.Rut);
        var hasChanges = dbContext.ChangeTracker.HasChanges();
        await dbContext.SaveChangesAsync(cancellationToken);

        if (hasChanges)
        {
            var savedCliente = await dbContext.Clientes
                .AsNoTracking()
                .Include(item => item.Negocio)
                .Include(item => item.Usuario)
                .FirstAsync(item => item.IdCliente == cliente.IdCliente, cancellationToken);

            await auditoriaService.RegistrarAsync(
                currentUser,
                new AuditoriaRegistro(
                    savedCliente.IdNegocio,
                    "Clientes",
                    isNew ? "CrearDesdeReservaPublica" : "ActualizarDesdeReservaPublica",
                    nameof(Cliente),
                    savedCliente.IdCliente.ToString(),
                    isNew
                        ? $"Cliente creado desde flujo de reserva pública: {savedCliente.Nombre}"
                        : $"Cliente actualizado desde flujo de reserva pública: {savedCliente.Nombre}",
                    previousSnapshot,
                    ToAuditSnapshot(savedCliente)),
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<ClienteReservaDto>.Success(ToDto(cliente));
    }

    private async Task<AuthenticatedUserData?> GetAuthenticatedUserDataAsync(
        CurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return null;
        }

        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == currentUser.UserId)
            .Select(user => new AuthenticatedUserData(user.Id, user.Email, user.NormalizedEmail))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static ValidationError? ValidateAuthenticatedEmail(
        string? requestEmail,
        AuthenticatedUserData authenticatedUser)
    {
        if (requestEmail is null)
        {
            return null;
        }

        var normalizedRequestEmail = NormalizeEmail(requestEmail);
        var normalizedUserEmail = authenticatedUser.NormalizedEmail ?? NormalizeEmail(authenticatedUser.Email ?? string.Empty);

        return string.Equals(normalizedRequestEmail, normalizedUserEmail, StringComparison.Ordinal)
            ? null
            : new ValidationError(nameof(ResolveClienteReservaRequest.Email), "El correo de la reserva debe coincidir con el correo del usuario autenticado.");
    }

    private async Task<Cliente?> FindClienteAsync(
        int idNegocio,
        string? userId,
        string? email,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var clienteByUser = await dbContext.Clientes.FirstOrDefaultAsync(
                cliente => cliente.IdNegocio == idNegocio && cliente.UserId == userId,
                cancellationToken);

            if (clienteByUser is not null)
            {
                return clienteByUser;
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail = NormalizeEmail(email);
        return await dbContext.Clientes.FirstOrDefaultAsync(
            cliente =>
                cliente.IdNegocio == idNegocio &&
                cliente.Email != null &&
                cliente.Email.Trim().ToUpper() == normalizedEmail &&
                (cliente.UserId == null || cliente.UserId == string.Empty || cliente.UserId == userId),
            cancellationToken);
    }

    private static void ApplyReservaData(
        Cliente cliente,
        string? userId,
        string nombre,
        string? email,
        string? telefono,
        string? rut)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            cliente.UserId = userId;
        }

        cliente.Nombre = TrimToMax(nombre, 150) ?? cliente.Nombre;

        var normalizedEmail = TrimToMax(email, 150);
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            cliente.Email = normalizedEmail;
        }

        var normalizedTelefono = TrimToMax(telefono, 30);
        if (!string.IsNullOrWhiteSpace(normalizedTelefono))
        {
            cliente.Telefono = normalizedTelefono;
        }

        var normalizedRut = TrimToMax(rut, 20);
        if (!string.IsNullOrWhiteSpace(normalizedRut))
        {
            cliente.Rut = normalizedRut;
        }

        cliente.Activo = true;
    }

    private static ClienteReservaDto ToDto(Cliente cliente)
    {
        return new ClienteReservaDto(
            cliente.IdCliente,
            cliente.IdNegocio,
            cliente.UserId,
            cliente.Nombre,
            cliente.Email,
            cliente.Telefono,
            cliente.Rut);
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
            cliente.Email,
            cliente.Telefono,
            cliente.Rut,
            cliente.Activo,
            cliente.FechaCreacion
        };
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? TrimToMax(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private sealed record AuthenticatedUserData(string UserId, string? Email, string? NormalizedEmail);
}
