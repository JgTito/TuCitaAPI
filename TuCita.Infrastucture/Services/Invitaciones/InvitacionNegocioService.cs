using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TuCita.Application.Auditoria;
using TuCita.Application.Auth;
using TuCita.Application.Common;
using TuCita.Application.Invitaciones;
using TuCita.Infrastucture.Authentication;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;
using TuCita.Infrastucture.UsuariosPerfil;

namespace TuCita.Infrastucture.Invitaciones;

public sealed class InvitacionNegocioService(
    ReservaFlowDbContext dbContext,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<JwtOptions> jwtOptions,
    IConfiguration configuration,
    IAuditoriaService auditoriaService) : IInvitacionNegocioService
{
    private const string RoleOwner = "Owner";
    private const string RoleAdmin = "Admin";
    private const string RoleRecepcionista = "Recepcionista";
    private const string RoleProfesional = "Profesional";
    private const string RoleCliente = "Cliente";
    private const string TipoInvitacionNegocioName = "InvitacionNegocio";
    private const string CanalEmailName = "Email";
    private const string EstadoNotificacionPendienteName = "Pendiente";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(48);
    private static readonly TimeSpan MaxExpiration = TimeSpan.FromHours(72);
    private static readonly CurrentUserContext SystemAuditUser = new(string.Empty, []);

    public async Task<PagedResult<InvitacionNegocioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        InvitacionNegocioQuery query,
        CancellationToken cancellationToken)
    {
        if (!await CanManageInvitationsAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<InvitacionNegocioDto>([], query.PageNumber, query.PageSize, 0);
        }

        await MarkExpiredPendingInvitationsAsync(idNegocio, cancellationToken);

        var invitationsQuery = BaseQuery(idNegocio).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Estado))
        {
            var estado = query.Estado.Trim();
            invitationsQuery = invitationsQuery.Where(item => item.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var email = query.Email.Trim();
            invitationsQuery = invitationsQuery.Where(item => item.Email.Contains(email));
        }

        if (query.FechaDesde.HasValue)
        {
            invitationsQuery = invitationsQuery.Where(item => item.FechaCreacion >= query.FechaDesde.Value);
        }

        if (query.FechaHasta.HasValue)
        {
            invitationsQuery = invitationsQuery.Where(item => item.FechaCreacion <= query.FechaHasta.Value);
        }

        var totalItems = await invitationsQuery.CountAsync(cancellationToken);
        var invitationItems = await invitationsQuery
            .OrderByDescending(item => item.FechaCreacion)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        var items = invitationItems.Select(ToDto).ToArray();

        return new PagedResult<InvitacionNegocioDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<InvitacionNegocioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idInvitacion,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateManagementAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var invitation = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdInvitacionNegocio == idInvitacion, cancellationToken);

        return invitation is null
            ? ServiceResult<InvitacionNegocioDto>.NotFound("La invitación no existe.")
            : ServiceResult<InvitacionNegocioDto>.Success(ToDto(invitation));
    }

    public async Task<PagedResult<InvitacionNegocioDto>> GetMineAsync(
        CurrentUserContext currentUser,
        MisInvitacionesQuery query,
        CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync(currentUser);
        if (user is null)
        {
            return new PagedResult<InvitacionNegocioDto>([], query.PageNumber, query.PageSize, 0);
        }

        var normalizedEmail = NormalizeEmail(user.Email);
        await MarkExpiredPendingInvitationsByEmailAsync(normalizedEmail, cancellationToken);

        var invitationsQuery = UserInboxQuery(normalizedEmail).AsNoTracking();

        if (string.IsNullOrWhiteSpace(query.Estado))
        {
            invitationsQuery = invitationsQuery.Where(item =>
                item.Estado == InvitacionNegocioEstados.Pendiente &&
                item.FechaExpiracion > DateTime.Now);
        }
        else
        {
            var estado = query.Estado.Trim();
            invitationsQuery = invitationsQuery.Where(item => item.Estado == estado);
        }

        var totalItems = await invitationsQuery.CountAsync(cancellationToken);
        var invitationItems = await invitationsQuery
            .OrderBy(item => item.FechaExpiracion)
            .ThenByDescending(item => item.FechaCreacion)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<InvitacionNegocioDto>(
            invitationItems.Select(ToDto).ToArray(),
            query.PageNumber,
            query.PageSize,
            totalItems);
    }

    public async Task<ServiceResult<InvitacionNegocioDto>> GetMineByIdAsync(
        CurrentUserContext currentUser,
        int idInvitacion,
        CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync(currentUser);
        if (user is null)
        {
            return ServiceResult<InvitacionNegocioDto>.Forbidden("Debes iniciar sesión para ver tus invitaciones.");
        }

        var invitation = await UserInboxQuery(NormalizeEmail(user.Email))
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdInvitacionNegocio == idInvitacion, cancellationToken);

        if (invitation is null)
        {
            return ServiceResult<InvitacionNegocioDto>.NotFound("La invitación no existe para el correo del usuario autenticado.");
        }

        return ServiceResult<InvitacionNegocioDto>.Success(ToDto(invitation));
    }

    public async Task<ServiceResult<InvitacionCreadaDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateInvitacionNegocioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateCreationAccessAsync(currentUser, idNegocio, request.IdRolNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var errors = await ValidateCreateAsync(idNegocio, request, cancellationToken);
        if (errors.Count > 0)
        {
            return ServiceResult<InvitacionCreadaDto>.Validation(errors);
        }

        var token = InvitationTokenGenerator.Generate();
        var normalizedEmail = NormalizeEmail(request.Email);
        var invitation = new InvitacionNegocio
        {
            IdNegocio = idNegocio,
            IdRolNegocio = request.IdRolNegocio,
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            TokenHash = TokenHasher.Hash(token),
            Estado = InvitacionNegocioEstados.Pendiente,
            InvitadoPorUserId = currentUser.UserId,
            FechaExpiracion = request.FechaExpiracion ?? DateTime.Now.Add(DefaultExpiration),
            Mensaje = request.Mensaje?.Trim()
        };

        dbContext.InvitacionesNegocio.Add(invitation);
        var acceptanceLink = BuildAcceptanceLink(token);
        await CrearNotificacionInvitacionAsync(invitation, acceptanceLink, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdInvitacionNegocio == invitation.IdInvitacionNegocio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "UsuariosNegocio",
                "CrearInvitacion",
                nameof(InvitacionNegocio),
                created.IdInvitacionNegocio.ToString(),
                $"Invitación de negocio creada para {created.Email} como {created.RolNegocio.Nombre}.",
                ValoresNuevos: ToInvitacionAuditSnapshot(created),
                Metadata: new { LinkGenerado = true, created.FechaExpiracion }),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<InvitacionCreadaDto>.Success(new InvitacionCreadaDto(
            ToDto(created),
            token,
            acceptanceLink));
    }

    public async Task<ServiceResult<InvitacionPreviewDto>> ValidateAsync(
        ValidateInvitacionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = DataAnnotationsValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<InvitacionPreviewDto>.Validation(validationErrors);
        }

        var invitation = await GetByTokenAsync(request.Token, tracking: true, cancellationToken);
        if (invitation is null)
        {
            return ServiceResult<InvitacionPreviewDto>.Success(InvalidPreview("La invitación no existe o el token no es válido."));
        }

        await MarkExpiredIfNeededAsync(invitation, cancellationToken);

        if (invitation.Estado != InvitacionNegocioEstados.Pendiente)
        {
            return ServiceResult<InvitacionPreviewDto>.Success(InvalidPreview($"La invitación está en estado {invitation.Estado}."));
        }

        return ServiceResult<InvitacionPreviewDto>.Success(new InvitacionPreviewDto(
            true,
            invitation.Email,
            invitation.Negocio.Nombre,
            invitation.RolNegocio.Nombre,
            invitation.FechaExpiracion,
            invitation.Estado,
            invitation.Mensaje));
    }

    public async Task<ServiceResult<InvitacionNegocioDto>> AcceptAsync(
        CurrentUserContext currentUser,
        AcceptInvitacionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = DataAnnotationsValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<InvitacionNegocioDto>.Validation(validationErrors);
        }

        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<InvitacionNegocioDto>.Forbidden("Debes iniciar sesión para aceptar la invitación.");
        }

        var invitation = await GetByTokenAsync(request.Token, tracking: true, cancellationToken);
        var validResult = await ValidateInvitationForAcceptanceAsync(invitation, cancellationToken);
        if (validResult is not null)
        {
            return validResult;
        }

        var user = await userManager.FindByIdAsync(currentUser.UserId);
        if (user is null)
        {
            return ServiceResult<InvitacionNegocioDto>.Forbidden("El usuario autenticado no existe.");
        }

        if (!string.Equals(NormalizeEmail(user.Email), invitation!.NormalizedEmail, StringComparison.Ordinal))
        {
            return ServiceResult<InvitacionNegocioDto>.Validation([
                new ValidationError(nameof(AcceptInvitacionRequest.Token), "Esta invitación pertenece a otro correo electrónico.")
            ]);
        }

        var relationExists = await dbContext.NegocioUsuarios.AnyAsync(
            item => item.IdNegocio == invitation.IdNegocio && item.UserId == user.Id && item.Activo,
            cancellationToken);

        if (relationExists)
        {
            return ServiceResult<InvitacionNegocioDto>.Validation([
                new ValidationError(nameof(AcceptInvitacionRequest.Token), "El usuario ya pertenece a este negocio.")
            ]);
        }

        var previousInvitationSnapshot = ToInvitacionAuditSnapshot(invitation!);
        await AddUserToBusinessAsync(invitation, user.Id, cancellationToken);
        await EnsureGlobalRoleAsync(user, invitation.RolNegocio.Nombre);
        MarkAccepted(invitation, user.Id);
        await RegistrarAceptacionInvitacionAsync(
            currentUser,
            invitation,
            previousInvitationSnapshot,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var accepted = await BaseQuery(invitation.IdNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdInvitacionNegocio == invitation.IdInvitacionNegocio, cancellationToken);

        return ServiceResult<InvitacionNegocioDto>.Success(ToDto(accepted));
    }

    public async Task<ServiceResult<InvitacionNegocioDto>> AcceptMineAsync(
        CurrentUserContext currentUser,
        int idInvitacion,
        CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync(currentUser);
        if (user is null)
        {
            return ServiceResult<InvitacionNegocioDto>.Forbidden("Debes iniciar sesión para aceptar la invitación.");
        }

        var invitation = await UserInboxQuery(NormalizeEmail(user.Email))
            .FirstOrDefaultAsync(item => item.IdInvitacionNegocio == idInvitacion, cancellationToken);

        var validResult = await ValidateInvitationForAcceptanceAsync(invitation, cancellationToken);
        if (validResult is not null)
        {
            return validResult;
        }

        var relationExists = await dbContext.NegocioUsuarios.AnyAsync(
            item => item.IdNegocio == invitation!.IdNegocio && item.UserId == user.Id && item.Activo,
            cancellationToken);

        if (relationExists)
        {
            return ServiceResult<InvitacionNegocioDto>.Validation([
                new ValidationError(nameof(idInvitacion), "El usuario ya pertenece a este negocio.")
            ]);
        }

        var previousInvitationSnapshot = ToInvitacionAuditSnapshot(invitation!);
        await AddUserToBusinessAsync(invitation!, user.Id, cancellationToken);
        await EnsureGlobalRoleAsync(user, invitation!.RolNegocio.Nombre);
        MarkAccepted(invitation!, user.Id);
        await RegistrarAceptacionInvitacionAsync(
            currentUser,
            invitation,
            previousInvitationSnapshot,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var accepted = await BaseQuery(invitation.IdNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdInvitacionNegocio == invitation.IdInvitacionNegocio, cancellationToken);

        return ServiceResult<InvitacionNegocioDto>.Success(ToDto(accepted));
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAndAcceptAsync(
        RegisterAndAcceptInvitacionRequest request,
        string? avatarUrl,
        CancellationToken cancellationToken)
    {
        var validationErrors = DataAnnotationsValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<AuthResponse>.Validation(validationErrors);
        }

        if (!request.AceptaTerminos)
        {
            return ServiceResult<AuthResponse>.Validation([
                new ValidationError(nameof(RegisterAndAcceptInvitacionRequest.AceptaTerminos), "Debes aceptar los términos y condiciones para registrarte.")
            ]);
        }

        if (!await ComunaIsValidAsync(request.IdComuna, cancellationToken))
        {
            return ServiceResult<AuthResponse>.Validation([
                new ValidationError(nameof(RegisterAndAcceptInvitacionRequest.IdComuna), "La comuna indicada no existe o no está activa.")
            ]);
        }

        var invitation = await GetByTokenAsync(request.Token, tracking: true, cancellationToken);
        var validResult = await ValidateInvitationForAcceptanceAsync(invitation, cancellationToken);
        if (validResult is not null)
        {
            return ServiceResult<AuthResponse>.Validation(validResult.ValidationErrors);
        }

        var existingUser = await userManager.FindByEmailAsync(invitation!.Email);
        if (existingUser is not null)
        {
            return ServiceResult<AuthResponse>.Validation([
                new ValidationError(nameof(RegisterAndAcceptInvitacionRequest.Token), "Ya existe una cuenta con este correo. Inicia sesión para aceptar la invitación.")
            ]);
        }

        var user = new IdentityUser
        {
            Email = invitation.Email,
            UserName = invitation.Email
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ServiceResult<AuthResponse>.Validation(
                createResult.Errors.Select(error => new ValidationError(nameof(RegisterAndAcceptInvitacionRequest.Password), error.Description)));
        }

        var profile = UsuarioPerfilFactory.Create(
            user.Id,
            request.Nombre,
            request.Apellido,
            request.Rut,
            request.FechaNacimiento,
            avatarUrl,
            request.TelefonoAlternativo,
            request.Direccion,
            request.IdComuna,
            request.AceptaTerminos,
            request.AceptaMarketing);
        var security = UsuarioPerfilFactory.EnsureSecurity(profile);
        security.FechaUltimoLogin = DateTime.UtcNow;
        security.UltimoAcceso = DateTime.UtcNow;
        dbContext.UsuariosPerfil.Add(profile);

        var previousInvitationSnapshot = ToInvitacionAuditSnapshot(invitation);
        await EnsureGlobalRoleAsync(user, invitation.RolNegocio.Nombre);
        await AddUserToBusinessAsync(invitation, user.Id, cancellationToken);
        MarkAccepted(invitation, user.Id);
        var auditUser = new CurrentUserContext(user.Id, [invitation.RolNegocio.Nombre]);
        await auditoriaService.RegistrarAsync(
            auditUser,
            new AuditoriaRegistro(
                invitation.IdNegocio,
                "Usuarios",
                "RegistrarUsuarioInvitado",
                nameof(IdentityUser),
                user.Id,
                $"Usuario registrado desde invitación de negocio: {invitation.Email}.",
                ValoresNuevos: new
                {
                    user.Id,
                    user.Email,
                    Roles = new[] { invitation.RolNegocio.Nombre },
                    Perfil = ToUsuarioPerfilAuditSnapshot(profile),
                    invitation.IdInvitacionNegocio
                }),
            cancellationToken);
        await RegistrarAceptacionInvitacionAsync(
            auditUser,
            invitation,
            previousInvitationSnapshot,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(await CreateAuthResponseAsync(user, cancellationToken, avatarUrl));
    }

    public async Task<ServiceResult<InvitacionNegocioDto>> CancelAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idInvitacion,
        CancelInvitacionRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateManagementAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var invitation = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdInvitacionNegocio == idInvitacion, cancellationToken);

        if (invitation is null)
        {
            return ServiceResult<InvitacionNegocioDto>.NotFound("La invitación no existe.");
        }

        if (invitation.Estado != InvitacionNegocioEstados.Pendiente)
        {
            return ServiceResult<InvitacionNegocioDto>.Validation([
                new ValidationError(nameof(CancelInvitacionRequest.Motivo), "Solo se pueden cancelar invitaciones pendientes.")
            ]);
        }

        var previousSnapshot = ToInvitacionAuditSnapshot(invitation);

        invitation.Estado = InvitacionNegocioEstados.Cancelada;
        invitation.FechaCancelacion = DateTime.Now;
        invitation.CanceladoPorUserId = currentUser.UserId;
        invitation.MotivoCancelacion = request.Motivo?.Trim();
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "UsuariosNegocio",
                "CancelarInvitacion",
                nameof(InvitacionNegocio),
                invitation.IdInvitacionNegocio.ToString(),
                $"Invitación de negocio cancelada para {invitation.Email}.",
                previousSnapshot,
                ToInvitacionAuditSnapshot(invitation),
                new { request.Motivo }),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<InvitacionNegocioDto>.Success(ToDto(invitation));
    }

    public async Task<ServiceResult<InvitacionCreadaDto>> ResendAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idInvitacion,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateManagementAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult.Status switch
            {
                ServiceResultStatus.NotFound => ServiceResult<InvitacionCreadaDto>.NotFound(accessResult.Errors.First()),
                ServiceResultStatus.Forbidden => ServiceResult<InvitacionCreadaDto>.Forbidden(accessResult.Errors.First()),
                _ => ServiceResult<InvitacionCreadaDto>.Validation(accessResult.ValidationErrors)
            };
        }

        var invitation = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdInvitacionNegocio == idInvitacion, cancellationToken);

        if (invitation is null)
        {
            return ServiceResult<InvitacionCreadaDto>.NotFound("La invitación no existe.");
        }

        if (invitation.Estado is InvitacionNegocioEstados.Aceptada or InvitacionNegocioEstados.Cancelada)
        {
            return ServiceResult<InvitacionCreadaDto>.Validation([
                new ValidationError(nameof(idInvitacion), "No se puede reenviar una invitación aceptada o cancelada.")
            ]);
        }

        var pendingDuplicateExists = await dbContext.InvitacionesNegocio.AnyAsync(
            item =>
                item.IdInvitacionNegocio != invitation.IdInvitacionNegocio &&
                item.IdNegocio == invitation.IdNegocio &&
                item.NormalizedEmail == invitation.NormalizedEmail &&
                item.Estado == InvitacionNegocioEstados.Pendiente,
            cancellationToken);

        if (pendingDuplicateExists)
        {
            return ServiceResult<InvitacionCreadaDto>.Validation([
                new ValidationError(nameof(idInvitacion), "Ya existe otra invitación pendiente para este correo.")
            ]);
        }

        var previousSnapshot = ToInvitacionAuditSnapshot(invitation);
        var token = InvitationTokenGenerator.Generate();
        invitation.TokenHash = TokenHasher.Hash(token);
        invitation.Estado = InvitacionNegocioEstados.Pendiente;
        invitation.FechaExpiracion = DateTime.Now.Add(DefaultExpiration);
        invitation.FechaUltimoReenvio = DateTime.Now;
        var acceptanceLink = BuildAcceptanceLink(token);
        await CrearNotificacionInvitacionAsync(invitation, acceptanceLink, cancellationToken);
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "UsuariosNegocio",
                "ReenviarInvitacion",
                nameof(InvitacionNegocio),
                invitation.IdInvitacionNegocio.ToString(),
                $"Invitación de negocio reenviada para {invitation.Email}.",
                previousSnapshot,
                ToInvitacionAuditSnapshot(invitation),
                new { LinkGenerado = true, invitation.FechaExpiracion }),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<InvitacionCreadaDto>.Success(new InvitacionCreadaDto(
            ToDto(invitation),
            token,
            acceptanceLink));
    }

    public async Task<ServiceResult<ExpirarInvitacionesResultDto>> ExpirarPendientesAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var expired = await dbContext.InvitacionesNegocio
            .Include(item => item.Negocio)
            .Include(item => item.RolNegocio)
            .Where(item =>
                item.Estado == InvitacionNegocioEstados.Pendiente &&
                item.FechaExpiracion <= now)
            .ToArrayAsync(cancellationToken);

        foreach (var invitation in expired)
        {
            var previousSnapshot = ToInvitacionAuditSnapshot(invitation);
            invitation.Estado = InvitacionNegocioEstados.Expirada;

            await RegistrarExpiracionInvitacionAsync(invitation, previousSnapshot, cancellationToken);
        }

        if (expired.Length > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<ExpirarInvitacionesResultDto>.Success(
            new ExpirarInvitacionesResultDto(expired.Length));
    }

    private async Task CrearNotificacionInvitacionAsync(
        InvitacionNegocio invitation,
        string acceptanceLink,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposNotificacion
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Nombre == TipoInvitacionNegocioName && item.Activo, cancellationToken);
        var canal = await dbContext.CanalesNotificacion
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Nombre == CanalEmailName && item.Activo, cancellationToken);
        var estado = await dbContext.EstadosNotificacion
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Nombre == EstadoNotificacionPendienteName && item.Activo, cancellationToken);

        if (tipo is null || canal is null || estado is null)
        {
            return;
        }

        var negocioNombre = invitation.Negocio?.Nombre;
        if (string.IsNullOrWhiteSpace(negocioNombre))
        {
            negocioNombre = await dbContext.Negocios
                .AsNoTracking()
                .Where(item => item.IdNegocio == invitation.IdNegocio)
                .Select(item => item.Nombre)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var negocio = string.IsNullOrWhiteSpace(negocioNombre) ? "TuCita" : negocioNombre.Trim();
        dbContext.Notificaciones.Add(new Notificacion
        {
            IdNegocio = invitation.IdNegocio,
            IdCita = null,
            IdTipoNotificacion = tipo.IdTipoNotificacion,
            IdCanalNotificacion = canal.IdCanalNotificacion,
            IdEstadoNotificacion = estado.IdEstadoNotificacion,
            Destinatario = invitation.Email.Trim(),
            Asunto = $"Invitación para unirte a {negocio}",
            Mensaje = acceptanceLink,
            FechaProgramada = DateTime.Now
        });
    }

    private IQueryable<InvitacionNegocio> BaseQuery(int idNegocio)
    {
        return dbContext.InvitacionesNegocio
            .Include(item => item.Negocio)
            .Include(item => item.RolNegocio)
            .Where(item => item.IdNegocio == idNegocio);
    }

    private IQueryable<InvitacionNegocio> UserInboxQuery(string normalizedEmail)
    {
        return dbContext.InvitacionesNegocio
            .Include(item => item.Negocio)
            .Include(item => item.RolNegocio)
            .Where(item => item.NormalizedEmail == normalizedEmail);
    }

    private async Task<ServiceResult<InvitacionCreadaDto>?> ValidateCreationAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idRolNegocio,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Negocios.AnyAsync(item => item.IdNegocio == idNegocio, cancellationToken))
        {
            return ServiceResult<InvitacionCreadaDto>.NotFound("El negocio no existe.");
        }

        var currentBusinessRole = await GetCurrentBusinessRoleAsync(currentUser, idNegocio, cancellationToken);
        if (currentBusinessRole is null)
        {
            return ServiceResult<InvitacionCreadaDto>.Forbidden("No tienes acceso para invitar usuarios a este negocio.");
        }

        var invitedRole = await dbContext.RolesNegocio
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdRolNegocio == idRolNegocio, cancellationToken);

        if (invitedRole is null)
        {
            return ServiceResult<InvitacionCreadaDto>.Validation([
                new ValidationError(nameof(CreateInvitacionNegocioRequest.IdRolNegocio), "El rol de negocio indicado no existe.")
            ]);
        }

        if (!CanInviteRole(currentBusinessRole, invitedRole.Nombre))
        {
            return ServiceResult<InvitacionCreadaDto>.Validation([
                new ValidationError(nameof(CreateInvitacionNegocioRequest.IdRolNegocio), "No puedes invitar usuarios con ese rol.")
            ]);
        }

        return null;
    }

    private async Task<ServiceResult<InvitacionNegocioDto>?> ValidateManagementAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Negocios.AnyAsync(item => item.IdNegocio == idNegocio, cancellationToken))
        {
            return ServiceResult<InvitacionNegocioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageInvitationsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<InvitacionNegocioDto>.Forbidden("No tienes acceso para administrar invitaciones de este negocio.");
        }

        return null;
    }

    private async Task<List<ValidationError>> ValidateCreateAsync(
        int idNegocio,
        CreateInvitacionNegocioRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var now = DateTime.Now;
        var expiration = request.FechaExpiracion ?? now.Add(DefaultExpiration);

        if (expiration <= now)
        {
            errors.Add(new ValidationError(nameof(CreateInvitacionNegocioRequest.FechaExpiracion), "La fecha de expiración debe ser posterior a la fecha actual."));
        }

        if (expiration > now.Add(MaxExpiration))
        {
            errors.Add(new ValidationError(nameof(CreateInvitacionNegocioRequest.FechaExpiracion), "La invitación no puede expirar en más de 72 horas."));
        }

        var roleExists = await dbContext.RolesNegocio.AnyAsync(
            item => item.IdRolNegocio == request.IdRolNegocio && item.Activo,
            cancellationToken);

        if (!roleExists)
        {
            errors.Add(new ValidationError(nameof(CreateInvitacionNegocioRequest.IdRolNegocio), "El rol de negocio indicado no existe o no está activo."));
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var invitedUser = await userManager.FindByEmailAsync(request.Email);
        if (invitedUser is not null)
        {
            var alreadyBelongs = await dbContext.NegocioUsuarios.AnyAsync(
                item => item.IdNegocio == idNegocio && item.UserId == invitedUser.Id && item.Activo,
                cancellationToken);

            if (alreadyBelongs)
            {
                errors.Add(new ValidationError(nameof(CreateInvitacionNegocioRequest.Email), "El usuario ya pertenece a este negocio."));
            }
        }

        var pendingInvitationExists = await dbContext.InvitacionesNegocio.AnyAsync(
            item =>
                item.IdNegocio == idNegocio &&
                item.NormalizedEmail == normalizedEmail &&
                item.Estado == InvitacionNegocioEstados.Pendiente,
            cancellationToken);

        if (pendingInvitationExists)
        {
            errors.Add(new ValidationError(nameof(CreateInvitacionNegocioRequest.Email), "Ya existe una invitación pendiente para este correo en el negocio."));
        }

        return errors;
    }

    private async Task<ServiceResult<InvitacionNegocioDto>?> ValidateInvitationForAcceptanceAsync(
        InvitacionNegocio? invitation,
        CancellationToken cancellationToken)
    {
        if (invitation is null)
        {
            return ServiceResult<InvitacionNegocioDto>.Validation([
                new ValidationError(nameof(AcceptInvitacionRequest.Token), "La invitación no existe o el token no es válido.")
            ]);
        }

        await MarkExpiredIfNeededAsync(invitation, cancellationToken);

        if (invitation.Estado != InvitacionNegocioEstados.Pendiente)
        {
            return ServiceResult<InvitacionNegocioDto>.Validation([
                new ValidationError(nameof(AcceptInvitacionRequest.Token), $"La invitación está en estado {invitation.Estado}.")
            ]);
        }

        return null;
    }

    private async Task<InvitacionNegocio?> GetByTokenAsync(
        string token,
        bool tracking,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = TokenHasher.Hash(token);
        var query = dbContext.InvitacionesNegocio
            .Include(item => item.Negocio)
            .Include(item => item.RolNegocio)
            .Where(item => item.TokenHash == tokenHash);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task MarkExpiredIfNeededAsync(InvitacionNegocio invitation, CancellationToken cancellationToken)
    {
        if (invitation.Estado == InvitacionNegocioEstados.Pendiente && invitation.FechaExpiracion <= DateTime.Now)
        {
            var previousSnapshot = ToInvitacionAuditSnapshot(invitation);
            invitation.Estado = InvitacionNegocioEstados.Expirada;
            await RegistrarExpiracionInvitacionAsync(invitation, previousSnapshot, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task MarkExpiredPendingInvitationsAsync(int idNegocio, CancellationToken cancellationToken)
    {
        var expired = await dbContext.InvitacionesNegocio
            .Include(item => item.Negocio)
            .Include(item => item.RolNegocio)
            .Where(item =>
                item.IdNegocio == idNegocio &&
                item.Estado == InvitacionNegocioEstados.Pendiente &&
                item.FechaExpiracion <= DateTime.Now)
            .ToArrayAsync(cancellationToken);

        if (expired.Length == 0)
        {
            return;
        }

        foreach (var invitation in expired)
        {
            var previousSnapshot = ToInvitacionAuditSnapshot(invitation);
            invitation.Estado = InvitacionNegocioEstados.Expirada;
            await RegistrarExpiracionInvitacionAsync(invitation, previousSnapshot, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkExpiredPendingInvitationsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var expired = await dbContext.InvitacionesNegocio
            .Include(item => item.Negocio)
            .Include(item => item.RolNegocio)
            .Where(item =>
                item.NormalizedEmail == normalizedEmail &&
                item.Estado == InvitacionNegocioEstados.Pendiente &&
                item.FechaExpiracion <= DateTime.Now)
            .ToArrayAsync(cancellationToken);

        if (expired.Length == 0)
        {
            return;
        }

        foreach (var invitation in expired)
        {
            var previousSnapshot = ToInvitacionAuditSnapshot(invitation);
            invitation.Estado = InvitacionNegocioEstados.Expirada;
            await RegistrarExpiracionInvitacionAsync(invitation, previousSnapshot, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IdentityUser?> GetAuthenticatedUserAsync(CurrentUserContext currentUser)
    {
        return currentUser.IsAuthenticated
            ? await userManager.FindByIdAsync(currentUser.UserId)
            : null;
    }

    private async Task AddUserToBusinessAsync(
        InvitacionNegocio invitation,
        string userId,
        CancellationToken cancellationToken)
    {
        var existingRelation = await dbContext.NegocioUsuarios
            .Include(item => item.Negocio)
            .Include(item => item.RolNegocio)
            .Include(item => item.Usuario)
            .FirstOrDefaultAsync(
            item => item.IdNegocio == invitation.IdNegocio && item.UserId == userId,
            cancellationToken);

        if (existingRelation is not null)
        {
            var previousSnapshot = ToNegocioUsuarioAuditSnapshot(existingRelation);
            existingRelation.IdRolNegocio = invitation.IdRolNegocio;
            existingRelation.Activo = true;
            await auditoriaService.RegistrarAsync(
                new CurrentUserContext(userId, []),
                new AuditoriaRegistro(
                    invitation.IdNegocio,
                    "UsuariosNegocio",
                    "AceptarInvitacion",
                    nameof(NegocioUsuario),
                    existingRelation.IdNegocioUsuario.ToString(),
                    $"Usuario reactivado por invitación: {invitation.Email}.",
                    previousSnapshot,
                    ToInvitacionNegocioUsuarioAuditSnapshot(invitation, userId, existingRelation.IdNegocioUsuario)),
                cancellationToken);
            return;
        }

        var relation = new NegocioUsuario
        {
            IdNegocio = invitation.IdNegocio,
            UserId = userId,
            IdRolNegocio = invitation.IdRolNegocio,
            Activo = true
        };

        dbContext.NegocioUsuarios.Add(relation);
        await auditoriaService.RegistrarAsync(
            new CurrentUserContext(userId, []),
            new AuditoriaRegistro(
                invitation.IdNegocio,
                "UsuariosNegocio",
                "AceptarInvitacion",
                nameof(NegocioUsuario),
                userId,
                $"Usuario asociado por invitación: {invitation.Email}.",
                ValoresNuevos: ToInvitacionNegocioUsuarioAuditSnapshot(invitation, userId, null)),
            cancellationToken);
    }

    private async Task EnsureGlobalRoleAsync(IdentityUser user, string roleName)
    {
        var targetRole = await roleManager.RoleExistsAsync(roleName)
            ? roleName
            : RoleCliente;

        if (!await roleManager.RoleExistsAsync(targetRole))
        {
            await roleManager.CreateAsync(new IdentityRole(targetRole));
        }

        if (!await userManager.IsInRoleAsync(user, targetRole))
        {
            await userManager.AddToRoleAsync(user, targetRole);
        }
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        IdentityUser user,
        CancellationToken cancellationToken,
        string? avatarUrl = null)
    {
        var roles = await userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);
        var token = CreateToken(user, roles, expiresAt);
        var refreshToken = RefreshTokenGenerator.Generate();
        var refreshTokenHash = TokenHasher.Hash(refreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays);

        dbContext.AuthRefreshTokens.Add(new AuthRefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = refreshTokenExpiresAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            roles.ToArray(),
            token,
            expiresAt,
            refreshToken,
            refreshTokenExpiresAt,
            avatarUrl ?? await GetAvatarUrlAsync(user.Id, cancellationToken));
    }

    private async Task<string?> GetAvatarUrlAsync(string userId, CancellationToken cancellationToken)
    {
        return await dbContext.UsuariosPerfil
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.AvatarUrl)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> ComunaIsValidAsync(int? idComuna, CancellationToken cancellationToken)
    {
        if (!idComuna.HasValue)
        {
            return true;
        }

        return await dbContext.Comunas.AnyAsync(
            item => item.IdComuna == idComuna.Value &&
                item.Activo &&
                item.Ciudad.Activo &&
                item.Ciudad.Pais.Activo,
            cancellationToken);
    }

    private string CreateToken(IdentityUser user, IEnumerable<string> roles, DateTime expiresAt)
    {
        var options = jwtOptions.Value;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<bool> CanManageInvitationsAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        return await GetCurrentBusinessRoleAsync(currentUser, idNegocio, cancellationToken) is not null;
    }

    private async Task<string?> GetCurrentBusinessRoleAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return "SuperAdmin";
        }

        if (!currentUser.IsAuthenticated)
        {
            return null;
        }

        return await dbContext.NegocioUsuarios
            .AsNoTracking()
            .Where(item =>
                item.IdNegocio == idNegocio &&
                item.UserId == currentUser.UserId &&
                item.Activo &&
                (item.RolNegocio.Nombre == RoleOwner || item.RolNegocio.Nombre == RoleAdmin))
            .Select(item => item.RolNegocio.Nombre)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool CanInviteRole(string currentRole, string invitedRole)
    {
        var allowedRoles = currentRole switch
        {
            "SuperAdmin" => new[] { RoleOwner, RoleAdmin, RoleRecepcionista, RoleProfesional },
            RoleOwner => [RoleAdmin, RoleRecepcionista, RoleProfesional],
            RoleAdmin => [RoleRecepcionista, RoleProfesional],
            _ => []
        };

        return allowedRoles.Contains(invitedRole, StringComparer.OrdinalIgnoreCase);
    }

    private void MarkAccepted(InvitacionNegocio invitation, string userId)
    {
        invitation.Estado = InvitacionNegocioEstados.Aceptada;
        invitation.FechaAceptacion = DateTime.Now;
        invitation.AceptadoPorUserId = userId;
    }

    private string BuildAcceptanceLink(string token)
    {
        var template = configuration["Invitations:AcceptUrl"];
        if (string.IsNullOrWhiteSpace(template))
        {
            return $"/invitaciones/aceptar?token={Uri.EscapeDataString(token)}";
        }

        return template.Contains("{token}", StringComparison.OrdinalIgnoreCase)
            ? template.Replace("{token}", Uri.EscapeDataString(token), StringComparison.OrdinalIgnoreCase)
            : $"{template}{(template.Contains('?') ? '&' : '?')}token={Uri.EscapeDataString(token)}";
    }

    private static InvitacionPreviewDto InvalidPreview(string message)
    {
        return new InvitacionPreviewDto(false, null, null, null, null, null, message);
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static InvitacionNegocioDto ToDto(InvitacionNegocio item)
    {
        return new InvitacionNegocioDto(
            item.IdInvitacionNegocio,
            item.IdNegocio,
            item.Negocio.Nombre,
            item.IdRolNegocio,
            item.RolNegocio.Nombre,
            item.Email,
            item.Estado,
            item.InvitadoPorUserId,
            item.AceptadoPorUserId,
            item.CanceladoPorUserId,
            item.FechaCreacion,
            item.FechaExpiracion,
            item.FechaAceptacion,
            item.FechaCancelacion,
            item.FechaUltimoReenvio,
            item.Mensaje,
            item.MotivoCancelacion);
    }

    private static object ToNegocioUsuarioAuditSnapshot(NegocioUsuario item)
    {
        return new
        {
            item.IdNegocioUsuario,
            item.IdNegocio,
            Negocio = item.Negocio.Nombre,
            item.UserId,
            Usuario = item.Usuario.Email ?? item.Usuario.UserName,
            item.IdRolNegocio,
            Rol = item.RolNegocio.Nombre,
            item.Activo,
            item.FechaCreacion
        };
    }

    private static object ToInvitacionAuditSnapshot(InvitacionNegocio invitation)
    {
        return new
        {
            invitation.IdInvitacionNegocio,
            invitation.IdNegocio,
            Negocio = invitation.Negocio?.Nombre,
            invitation.IdRolNegocio,
            Rol = invitation.RolNegocio?.Nombre,
            invitation.Email,
            invitation.NormalizedEmail,
            invitation.Estado,
            invitation.InvitadoPorUserId,
            invitation.AceptadoPorUserId,
            invitation.CanceladoPorUserId,
            invitation.FechaCreacion,
            invitation.FechaExpiracion,
            invitation.FechaAceptacion,
            invitation.FechaCancelacion,
            invitation.FechaUltimoReenvio,
            invitation.Mensaje,
            invitation.MotivoCancelacion
        };
    }

    private async Task RegistrarExpiracionInvitacionAsync(
        InvitacionNegocio invitation,
        object previousSnapshot,
        CancellationToken cancellationToken)
    {
        await auditoriaService.RegistrarAsync(
            SystemAuditUser,
            new AuditoriaRegistro(
                invitation.IdNegocio,
                "UsuariosNegocio",
                "ExpirarInvitacion",
                nameof(InvitacionNegocio),
                invitation.IdInvitacionNegocio.ToString(),
                $"Invitación de negocio expirada automáticamente para {invitation.Email}.",
                previousSnapshot,
                ToInvitacionAuditSnapshot(invitation)),
            cancellationToken);
    }

    private async Task RegistrarAceptacionInvitacionAsync(
        CurrentUserContext currentUser,
        InvitacionNegocio invitation,
        object previousSnapshot,
        CancellationToken cancellationToken)
    {
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                invitation.IdNegocio,
                "UsuariosNegocio",
                "AceptarInvitacion",
                nameof(InvitacionNegocio),
                invitation.IdInvitacionNegocio.ToString(),
                $"Invitación de negocio aceptada por {invitation.Email}.",
                previousSnapshot,
                ToInvitacionAuditSnapshot(invitation)),
            cancellationToken);
    }

    private static object ToUsuarioPerfilAuditSnapshot(UsuarioPerfil profile)
    {
        return new
        {
            profile.UserId,
            profile.Nombre,
            profile.Apellido,
            profile.NombreCompleto,
            profile.Rut,
            profile.FechaNacimiento,
            profile.AvatarUrl,
            TelefonoAlternativo = profile.Contacto?.TelefonoAlternativo,
            Direccion = profile.Direccion?.Direccion,
            IdComuna = profile.Direccion?.IdComuna,
            profile.Activo
        };
    }

    private static object ToInvitacionNegocioUsuarioAuditSnapshot(
        InvitacionNegocio invitation,
        string userId,
        int? idNegocioUsuario)
    {
        return new
        {
            IdNegocioUsuario = idNegocioUsuario,
            invitation.IdNegocio,
            Negocio = invitation.Negocio.Nombre,
            UserId = userId,
            Usuario = invitation.Email,
            invitation.IdRolNegocio,
            Rol = invitation.RolNegocio.Nombre,
            Activo = true,
            IdInvitacionNegocio = invitation.IdInvitacionNegocio,
            invitation.FechaAceptacion
        };
    }
}
