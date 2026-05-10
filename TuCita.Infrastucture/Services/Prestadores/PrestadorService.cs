using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.Prestadores;
using TuCita.Infrastucture.Authentication;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Prestadores;

public sealed class PrestadorService(
    ReservaFlowDbContext dbContext,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration,
    IAuditoriaService auditoriaService) : IPrestadorService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const string RecepcionistaRoleName = "Recepcionista";
    private const string ProfesionalRoleName = "Profesional";
    private const string TipoPrestadorProfesionalName = "Profesional";
    private const string TipoInvitacionNegocioName = "InvitacionNegocio";
    private const string CanalEmailName = "Email";
    private const string EstadoNotificacionPendienteName = "Pendiente";
    private static readonly TimeSpan DefaultInvitationExpiration = TimeSpan.FromHours(48);

    public async Task<PagedResult<PrestadorDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PrestadorQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<PrestadorDto>([], query.PageNumber, query.PageSize, 0);
        }

        var prestadoresQuery = BaseQuery(idNegocio).AsNoTracking();
        prestadoresQuery = ApplyFilters(prestadoresQuery, query, allowNegocioFilter: false);

        return await ToPagedResultAsync(prestadoresQuery, query, cancellationToken);
    }

    public async Task<PagedResult<PrestadorDto>> GetFromAssociatedBusinessesAsync(
        CurrentUserContext currentUser,
        PrestadorQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return new PagedResult<PrestadorDto>([], query.PageNumber, query.PageSize, 0);
        }

        var prestadoresQuery = BaseQuery()
            .AsNoTracking()
            .Where(prestador => prestador.Negocio.NegocioUsuarios.Any(usuario =>
                usuario.UserId == currentUser.UserId &&
                usuario.Activo));

        prestadoresQuery = ApplyFilters(prestadoresQuery, query, allowNegocioFilter: true);

        return await ToPagedResultAsync(prestadoresQuery, query, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PrestadorSelectDto>> GetSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PrestadorSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanReadSelectAsync(currentUser, idNegocio, cancellationToken))
        {
            return [];
        }

        var prestadoresQuery = BaseQuery(idNegocio).AsNoTracking();
        prestadoresQuery = ApplySelectFilters(prestadoresQuery, query, allowNegocioFilter: false);

        return await ToSelectListAsync(prestadoresQuery, includeBusinessInLabel: false, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PrestadorSelectDto>> GetSelectFromAssociatedBusinessesAsync(
        CurrentUserContext currentUser,
        PrestadorSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return [];
        }

        var prestadoresQuery = BaseQuery()
            .AsNoTracking()
            .Where(prestador => prestador.Negocio.NegocioUsuarios.Any(usuario =>
                usuario.UserId == currentUser.UserId &&
                usuario.Activo));

        prestadoresQuery = ApplySelectFilters(prestadoresQuery, query, allowNegocioFilter: true);

        return await ToSelectListAsync(prestadoresQuery, includeBusinessInLabel: true, cancellationToken);
    }

    public async Task<ServiceResult<PrestadorDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var prestador = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdPrestador == idPrestador, cancellationToken);

        return prestador is null
            ? ServiceResult<PrestadorDto>.NotFound("El prestador o recurso no existe.")
            : ServiceResult<PrestadorDto>.Success(ToDto(prestador));
    }

    public async Task<ServiceResult<PrestadorDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreatePrestadorRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var validationErrors = await ValidateRequestAsync(
            request.IdTipoPrestador,
            request.UserId,
            request.Capacidad,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<PrestadorDto>.Validation(validationErrors);
        }

        var prestador = new Prestador
        {
            IdNegocio = idNegocio,
            IdTipoPrestador = request.IdTipoPrestador,
            UserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim(),
            Nombre = request.Nombre.Trim(),
            Email = request.Email?.Trim(),
            Telefono = request.Telefono?.Trim(),
            Capacidad = request.Capacidad,
            Activo = request.Activo
        };

        await VincularUsuarioOInvitarProfesionalAsync(currentUser, prestador, cancellationToken);

        dbContext.Prestadores.Add(prestador);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdPrestador == prestador.IdPrestador, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Prestadores",
                "Crear",
                nameof(Prestador),
                created.IdPrestador.ToString(),
                $"Prestador/recurso creado: {created.Nombre}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<PrestadorDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<PrestadorDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        UpdatePrestadorRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var prestador = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdPrestador == idPrestador, cancellationToken);

        if (prestador is null)
        {
            return ServiceResult<PrestadorDto>.NotFound("El prestador o recurso no existe.");
        }

        var validationErrors = await ValidateRequestAsync(
            request.IdTipoPrestador,
            request.UserId,
            request.Capacidad,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<PrestadorDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(prestador);

        prestador.IdTipoPrestador = request.IdTipoPrestador;
        prestador.UserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim();
        prestador.Nombre = request.Nombre.Trim();
        prestador.Email = request.Email?.Trim();
        prestador.Telefono = request.Telefono?.Trim();
        prestador.Capacidad = request.Capacidad;
        prestador.Activo = request.Activo;

        await VincularUsuarioOInvitarProfesionalAsync(currentUser, prestador, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdPrestador == idPrestador, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Prestadores",
                "Editar",
                nameof(Prestador),
                updated.IdPrestador.ToString(),
                $"Prestador/recurso editado: {updated.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<PrestadorDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<PrestadorDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        bool activo,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var prestador = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdPrestador == idPrestador, cancellationToken);

        if (prestador is null)
        {
            return ServiceResult<PrestadorDto>.NotFound("El prestador o recurso no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(prestador);
        prestador.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdPrestador == idPrestador, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Prestadores",
                activo ? "Activar" : "Desactivar",
                nameof(Prestador),
                updated.IdPrestador.ToString(),
                activo ? $"Prestador/recurso activado: {updated.Nombre}" : $"Prestador/recurso desactivado: {updated.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<PrestadorDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<PrestadorDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, idPrestador, activo: false, cancellationToken);
    }

    private static IQueryable<Prestador> ApplyFilters(
        IQueryable<Prestador> prestadoresQuery,
        PrestadorQuery query,
        bool allowNegocioFilter)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            prestadoresQuery = prestadoresQuery.Where(prestador =>
                prestador.Nombre.Contains(search) ||
                (prestador.Email != null && prestador.Email.Contains(search)) ||
                (prestador.Telefono != null && prestador.Telefono.Contains(search)) ||
                prestador.TipoPrestador.Nombre.Contains(search) ||
                prestador.Negocio.Nombre.Contains(search) ||
                (prestador.Usuario != null && prestador.Usuario.Email != null && prestador.Usuario.Email.Contains(search)) ||
                (prestador.Usuario != null && prestador.Usuario.UserName != null && prestador.Usuario.UserName.Contains(search)));
        }

        if (allowNegocioFilter && query.IdNegocio.HasValue)
        {
            prestadoresQuery = prestadoresQuery.Where(prestador => prestador.IdNegocio == query.IdNegocio.Value);
        }

        if (query.IdTipoPrestador.HasValue)
        {
            prestadoresQuery = prestadoresQuery.Where(prestador => prestador.IdTipoPrestador == query.IdTipoPrestador.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            var userId = query.UserId.Trim();
            prestadoresQuery = prestadoresQuery.Where(prestador => prestador.UserId == userId);
        }

        if (query.Activo.HasValue)
        {
            prestadoresQuery = prestadoresQuery.Where(prestador => prestador.Activo == query.Activo.Value);
        }

        return prestadoresQuery;
    }

    private static IQueryable<Prestador> ApplySelectFilters(
        IQueryable<Prestador> prestadoresQuery,
        PrestadorSelectQuery query,
        bool allowNegocioFilter)
    {
        if (query.SoloActivos)
        {
            prestadoresQuery = prestadoresQuery.Where(prestador => prestador.Activo);
        }

        if (allowNegocioFilter && query.IdNegocio.HasValue)
        {
            prestadoresQuery = prestadoresQuery.Where(prestador => prestador.IdNegocio == query.IdNegocio.Value);
        }

        if (query.IdTipoPrestador.HasValue)
        {
            prestadoresQuery = prestadoresQuery.Where(prestador => prestador.IdTipoPrestador == query.IdTipoPrestador.Value);
        }

        if (query.IdServicio.HasValue)
        {
            prestadoresQuery = prestadoresQuery.Where(prestador => prestador.PrestadorServicios.Any(relacion =>
                relacion.IdServicio == query.IdServicio.Value &&
                relacion.Activo));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            prestadoresQuery = prestadoresQuery.Where(prestador =>
                prestador.Nombre.Contains(search) ||
                prestador.Negocio.Nombre.Contains(search) ||
                prestador.TipoPrestador.Nombre.Contains(search) ||
                (prestador.Email != null && prestador.Email.Contains(search)) ||
                (prestador.Usuario != null && prestador.Usuario.Email != null && prestador.Usuario.Email.Contains(search)) ||
                (prestador.Usuario != null && prestador.Usuario.UserName != null && prestador.Usuario.UserName.Contains(search)));
        }

        return prestadoresQuery;
    }

    private static async Task<IReadOnlyCollection<PrestadorSelectDto>> ToSelectListAsync(
        IQueryable<Prestador> prestadoresQuery,
        bool includeBusinessInLabel,
        CancellationToken cancellationToken)
    {
        return await prestadoresQuery
            .OrderBy(prestador => prestador.Negocio.Nombre)
            .ThenBy(prestador => prestador.Nombre)
            .Select(prestador => new PrestadorSelectDto(
                prestador.IdPrestador,
                prestador.IdNegocio,
                prestador.Negocio.Nombre,
                includeBusinessInLabel
                    ? prestador.Negocio.Nombre + " - " + prestador.Nombre
                    : prestador.Nombre,
                prestador.Nombre,
                prestador.IdTipoPrestador,
                prestador.TipoPrestador.Nombre,
                prestador.UserId,
                prestador.Capacidad,
                prestador.Activo))
            .ToArrayAsync(cancellationToken);
    }

    private static async Task<PagedResult<PrestadorDto>> ToPagedResultAsync(
        IQueryable<Prestador> prestadoresQuery,
        PrestadorQuery query,
        CancellationToken cancellationToken)
    {
        var totalItems = await prestadoresQuery.CountAsync(cancellationToken);
        var items = await prestadoresQuery
            .OrderBy(prestador => prestador.Negocio.Nombre)
            .ThenBy(prestador => prestador.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(prestador => new PrestadorDto(
                prestador.IdPrestador,
                prestador.IdNegocio,
                prestador.Negocio.Nombre,
                prestador.IdTipoPrestador,
                prestador.TipoPrestador.Nombre,
                prestador.UserId,
                prestador.Usuario != null ? prestador.Usuario.UserName : null,
                prestador.Usuario != null ? prestador.Usuario.Email : null,
                prestador.Nombre,
                prestador.Email,
                prestador.Telefono,
                prestador.Capacidad,
                prestador.Activo,
                prestador.FechaCreacion))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<PrestadorDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    private IQueryable<Prestador> BaseQuery()
    {
        return dbContext.Prestadores
            .Include(prestador => prestador.Negocio)
                .ThenInclude(negocio => negocio.NegocioUsuarios)
            .Include(prestador => prestador.TipoPrestador)
            .Include(prestador => prestador.Usuario);
    }

    private IQueryable<Prestador> BaseQuery(int idNegocio)
    {
        return BaseQuery()
            .Where(prestador => prestador.IdNegocio == idNegocio);
    }

    private async Task<ServiceResult<PrestadorDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<PrestadorDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<PrestadorDto>.Forbidden("No tienes acceso para administrar prestadores o recursos de este negocio.");
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

    private async Task VincularUsuarioOInvitarProfesionalAsync(
        CurrentUserContext currentUser,
        Prestador prestador,
        CancellationToken cancellationToken)
    {
        if (!await IsTipoPrestadorProfesionalAsync(prestador.IdTipoPrestador, cancellationToken))
        {
            return;
        }

        var idRolProfesional = await GetRolProfesionalIdAsync(cancellationToken);
        if (!idRolProfesional.HasValue)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(prestador.UserId))
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(
                item => item.Id == prestador.UserId.Trim(),
                cancellationToken);

            if (user is not null)
            {
                await EnsureProfessionalMembershipAsync(currentUser, prestador.IdNegocio, user.Id, idRolProfesional.Value, cancellationToken);
                await EnsureGlobalProfessionalRoleAsync(user);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(prestador.Email))
        {
            return;
        }

        var email = prestador.Email.Trim();
        var normalizedEmail = NormalizeEmail(email);
        var existingUser = await dbContext.Users.FirstOrDefaultAsync(
            user => user.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (existingUser is not null)
        {
            prestador.UserId = existingUser.Id;
            await EnsureProfessionalMembershipAsync(currentUser, prestador.IdNegocio, existingUser.Id, idRolProfesional.Value, cancellationToken);
            await EnsureGlobalProfessionalRoleAsync(existingUser);
            return;
        }

        await EnsurePendingProfessionalInvitationAsync(
            currentUser,
            prestador.IdNegocio,
            idRolProfesional.Value,
            email,
            prestador.Nombre,
            cancellationToken);
    }

    private async Task<bool> IsTipoPrestadorProfesionalAsync(int idTipoPrestador, CancellationToken cancellationToken)
    {
        return await dbContext.TiposPrestador.AnyAsync(
            tipo =>
                tipo.IdTipoPrestador == idTipoPrestador &&
                tipo.Nombre == TipoPrestadorProfesionalName &&
                tipo.Activo,
            cancellationToken);
    }

    private async Task<int?> GetRolProfesionalIdAsync(CancellationToken cancellationToken)
    {
        return await dbContext.RolesNegocio
            .AsNoTracking()
            .Where(role => role.Nombre == ProfesionalRoleName && role.Activo)
            .Select(role => (int?)role.IdRolNegocio)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task EnsureProfessionalMembershipAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        string userId,
        int idRolProfesional,
        CancellationToken cancellationToken)
    {
        var relation = await dbContext.NegocioUsuarios.FirstOrDefaultAsync(
            item => item.IdNegocio == idNegocio && item.UserId == userId,
            cancellationToken);

        if (relation is not null)
        {
            if (relation.Activo)
            {
                return;
            }

            var previousSnapshot = ToNegocioUsuarioAuditSnapshot(relation);
            relation.Activo = true;

            await auditoriaService.RegistrarAsync(
                currentUser,
                new AuditoriaRegistro(
                    idNegocio,
                    "UsuariosNegocio",
                    "VincularProfesionalPorPrestador",
                    nameof(NegocioUsuario),
                    relation.IdNegocioUsuario.ToString(),
                    "Usuario vinculado como profesional desde el mantenedor de prestadores.",
                    previousSnapshot,
                    ToNegocioUsuarioAuditSnapshot(relation)),
                cancellationToken);

            return;
        }

        var newRelation = new NegocioUsuario
        {
            IdNegocio = idNegocio,
            UserId = userId,
            IdRolNegocio = idRolProfesional,
            Activo = true
        };

        dbContext.NegocioUsuarios.Add(newRelation);
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "UsuariosNegocio",
                "VincularProfesionalPorPrestador",
                nameof(NegocioUsuario),
                userId,
                "Usuario asociado como profesional desde el mantenedor de prestadores.",
                ValoresNuevos: ToNegocioUsuarioAuditSnapshot(newRelation)),
            cancellationToken);
    }

    private async Task EnsureGlobalProfessionalRoleAsync(IdentityUser user)
    {
        if (!await roleManager.RoleExistsAsync(ProfesionalRoleName))
        {
            await roleManager.CreateAsync(new IdentityRole(ProfesionalRoleName));
        }

        if (!await userManager.IsInRoleAsync(user, ProfesionalRoleName))
        {
            await userManager.AddToRoleAsync(user, ProfesionalRoleName);
        }
    }

    private async Task EnsurePendingProfessionalInvitationAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idRolProfesional,
        string email,
        string nombrePrestador,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return;
        }

        var normalizedEmail = NormalizeEmail(email);
        var now = DateTime.Now;
        var expiredPendingInvitations = await dbContext.InvitacionesNegocio
            .Include(invitation => invitation.Negocio)
            .Include(invitation => invitation.RolNegocio)
            .Where(invitation =>
                invitation.IdNegocio == idNegocio &&
                invitation.NormalizedEmail == normalizedEmail &&
                invitation.Estado == InvitacionNegocioEstados.Pendiente &&
                invitation.FechaExpiracion <= now)
            .ToArrayAsync(cancellationToken);

        foreach (var invitation in expiredPendingInvitations)
        {
            var previousSnapshot = ToInvitacionAuditSnapshot(invitation);
            invitation.Estado = InvitacionNegocioEstados.Expirada;

            await auditoriaService.RegistrarAsync(
                currentUser,
                new AuditoriaRegistro(
                    idNegocio,
                    "UsuariosNegocio",
                    "ExpirarInvitacionProfesionalPrevia",
                    nameof(InvitacionNegocio),
                    invitation.IdInvitacionNegocio.ToString(),
                    $"Invitación profesional previa expirada para {invitation.Email}.",
                    previousSnapshot,
                    ToInvitacionAuditSnapshot(invitation)),
                cancellationToken);
        }

        var activePendingExists = await dbContext.InvitacionesNegocio.AnyAsync(
            invitation =>
                invitation.IdNegocio == idNegocio &&
                invitation.NormalizedEmail == normalizedEmail &&
                invitation.Estado == InvitacionNegocioEstados.Pendiente &&
                invitation.FechaExpiracion > now,
            cancellationToken);

        if (activePendingExists)
        {
            return;
        }

        var token = InvitationTokenGenerator.Generate();
        var acceptanceLink = BuildAcceptanceLink(token);
        var invitationMessage = string.IsNullOrWhiteSpace(nombrePrestador)
            ? "Invitación generada al registrar un prestador profesional."
            : $"Invitación generada al registrar al prestador profesional {nombrePrestador.Trim()}.";

        var fechaExpiracion = now.Add(DefaultInvitationExpiration);
        dbContext.InvitacionesNegocio.Add(new InvitacionNegocio
        {
            IdNegocio = idNegocio,
            IdRolNegocio = idRolProfesional,
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            TokenHash = TokenHasher.Hash(token),
            Estado = InvitacionNegocioEstados.Pendiente,
            InvitadoPorUserId = currentUser.UserId,
            FechaExpiracion = fechaExpiracion,
            Mensaje = invitationMessage
        });

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "UsuariosNegocio",
                "InvitarProfesionalPorPrestador",
                nameof(InvitacionNegocio),
                normalizedEmail,
                $"Invitación profesional generada desde el mantenedor de prestadores para {email.Trim()}.",
                ValoresNuevos: new
                {
                    IdNegocio = idNegocio,
                    IdRolNegocio = idRolProfesional,
                    Email = email.Trim(),
                    NormalizedEmail = normalizedEmail,
                    Estado = InvitacionNegocioEstados.Pendiente,
                    InvitadoPorUserId = currentUser.UserId,
                    FechaExpiracion = fechaExpiracion,
                    Mensaje = invitationMessage
                },
                Metadata: new
                {
                    GeneradaPorPrestador = true,
                    TokenHashGuardado = true,
                    LinkEnviadoPorNotificacion = true
                }),
            cancellationToken);

        await CrearNotificacionInvitacionAsync(idNegocio, email, acceptanceLink, cancellationToken);
    }

    private async Task CrearNotificacionInvitacionAsync(
        int idNegocio,
        string email,
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

        var negocioNombre = await dbContext.Negocios
            .AsNoTracking()
            .Where(item => item.IdNegocio == idNegocio)
            .Select(item => item.Nombre)
            .FirstOrDefaultAsync(cancellationToken);
        var negocio = string.IsNullOrWhiteSpace(negocioNombre) ? "TuCita" : negocioNombre.Trim();

        dbContext.Notificaciones.Add(new Notificacion
        {
            IdNegocio = idNegocio,
            IdCita = null,
            IdTipoNotificacion = tipo.IdTipoNotificacion,
            IdCanalNotificacion = canal.IdCanalNotificacion,
            IdEstadoNotificacion = estado.IdEstadoNotificacion,
            Destinatario = email.Trim(),
            Asunto = $"Invitación para unirte a {negocio}",
            Mensaje = acceptanceLink,
            FechaProgramada = DateTime.Now
        });
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

    private async Task<List<ValidationError>> ValidateRequestAsync(
        int idTipoPrestador,
        string? userId,
        int capacidad,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        var tipoExists = await dbContext.TiposPrestador.AnyAsync(
            tipo => tipo.IdTipoPrestador == idTipoPrestador && tipo.Activo,
            cancellationToken);

        if (!tipoExists)
        {
            errors.Add(new ValidationError(nameof(CreatePrestadorRequest.IdTipoPrestador), "El tipo de prestador indicado no existe o no está activo."));
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var trimmedUserId = userId.Trim();
            var userExists = await dbContext.Users.AnyAsync(user => user.Id == trimmedUserId, cancellationToken);

            if (!userExists)
            {
                errors.Add(new ValidationError(nameof(CreatePrestadorRequest.UserId), "El usuario indicado no existe."));
            }
        }

        if (capacidad <= 0)
        {
            errors.Add(new ValidationError(nameof(CreatePrestadorRequest.Capacidad), "La capacidad debe ser mayor a cero."));
        }

        return errors;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static PrestadorDto ToDto(Prestador prestador)
    {
        return new PrestadorDto(
            prestador.IdPrestador,
            prestador.IdNegocio,
            prestador.Negocio.Nombre,
            prestador.IdTipoPrestador,
            prestador.TipoPrestador.Nombre,
            prestador.UserId,
            prestador.Usuario?.UserName,
            prestador.Usuario?.Email,
            prestador.Nombre,
            prestador.Email,
            prestador.Telefono,
            prestador.Capacidad,
            prestador.Activo,
            prestador.FechaCreacion);
    }

    private static object ToAuditSnapshot(Prestador prestador)
    {
        return new
        {
            prestador.IdPrestador,
            prestador.IdNegocio,
            Negocio = prestador.Negocio?.Nombre,
            prestador.IdTipoPrestador,
            TipoPrestador = prestador.TipoPrestador?.Nombre,
            prestador.UserId,
            Usuario = prestador.Usuario?.Email ?? prestador.Usuario?.UserName,
            prestador.Nombre,
            prestador.Email,
            prestador.Telefono,
            prestador.Capacidad,
            prestador.Activo,
            prestador.FechaCreacion
        };
    }

    private static object ToNegocioUsuarioAuditSnapshot(NegocioUsuario relacion)
    {
        return new
        {
            relacion.IdNegocioUsuario,
            relacion.IdNegocio,
            relacion.UserId,
            relacion.IdRolNegocio,
            relacion.Activo,
            relacion.FechaCreacion
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
}
