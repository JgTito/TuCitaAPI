using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.Resenas;
using TuCita.Infrastucture.Authentication;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Resenas;

public sealed class ResenaNegocioService(
    ReservaFlowDbContext dbContext,
    UserManager<IdentityUser> userManager,
    IConfiguration configuration,
    IAuditoriaService auditoriaService) : IResenaNegocioService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const string RecepcionistaRoleName = "Recepcionista";
    private const string AtendidaStateName = "Atendida";
    private const string CanalEmailName = "Email";
    private const string EstadoNotificacionPendienteName = "Pendiente";
    private const string TipoPostAtencionName = "PostAtencion";
    private const string TipoNuevaResenaNegocioName = "NuevaResenaNegocio";
    private const string TipoAlertaResenaNegocioName = "AlertaResenaNegocio";
    private static readonly string[] EstadosResena =
    [
        ResenaNegocioEstados.Pendiente,
        ResenaNegocioEstados.Aprobada,
        ResenaNegocioEstados.Rechazada,
        ResenaNegocioEstados.Oculta
    ];

    public async Task<ServiceResult<PagedResult<ResenaNegocioDto>>> GetByNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ResenaNegocioQuery query,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(currentUser, idNegocio, cancellationToken);
        if (!access.Exists)
        {
            return ServiceResult<PagedResult<ResenaNegocioDto>>.NotFound("El negocio no existe.");
        }

        if (!access.CanView)
        {
            return ServiceResult<PagedResult<ResenaNegocioDto>>.Forbidden("No tienes acceso para ver las reseñas de este negocio.");
        }

        var resenasQuery = ApplyBusinessScope(BaseQuery(idNegocio).AsNoTracking(), access);
        resenasQuery = ApplyFilters(resenasQuery, query);

        var totalItems = await resenasQuery.CountAsync(cancellationToken);
        var items = await resenasQuery
            .OrderByDescending(item => item.FechaCreacion)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return ServiceResult<PagedResult<ResenaNegocioDto>>.Success(
            new PagedResult<ResenaNegocioDto>(
                items.Select(ToDto).ToArray(),
                query.PageNumber,
                query.PageSize,
                totalItems));
    }

    public async Task<ServiceResult<ResenaResumenDto>> GetResumenNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(currentUser, idNegocio, cancellationToken);
        if (!access.Exists)
        {
            return ServiceResult<ResenaResumenDto>.NotFound("El negocio no existe.");
        }

        if (!access.CanView)
        {
            return ServiceResult<ResenaResumenDto>.Forbidden("No tienes acceso para ver el resumen de reseñas de este negocio.");
        }

        var resenas = await ApplyBusinessScope(BaseQuery(idNegocio).AsNoTracking(), access)
            .Where(item => item.Activo)
            .ToArrayAsync(cancellationToken);

        var negocio = await dbContext.Negocios
            .AsNoTracking()
            .Where(item => item.IdNegocio == idNegocio)
            .Select(item => item.Nombre)
            .FirstAsync(cancellationToken);

        return ServiceResult<ResenaResumenDto>.Success(BuildResumen(idNegocio, negocio, resenas));
    }

    public async Task<ServiceResult<ReputacionNegocioDto>> GetReputacionAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ReputacionNegocioQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<ReputacionNegocioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageConfigurationAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<ReputacionNegocioDto>.Forbidden("No tienes acceso para ver la reputación global de este negocio.");
        }

        var period = ResolveReputationPeriod(query);
        if (period.FechaHasta < period.FechaDesde)
        {
            return ServiceResult<ReputacionNegocioDto>.Validation([
                new ValidationError(nameof(ReputacionNegocioQuery.FechaHasta), "La fecha hasta no puede ser anterior a la fecha desde.")
            ]);
        }

        var negocio = await dbContext.Negocios
            .AsNoTracking()
            .Where(item => item.IdNegocio == idNegocio)
            .Select(item => item.Nombre)
            .FirstAsync(cancellationToken);

        var resenasQuery = BaseQuery(idNegocio)
            .AsNoTracking()
            .Where(item =>
                item.Activo &&
                (item.FechaPublicacion ?? item.FechaCreacion) >= period.FechaDesde &&
                (item.FechaPublicacion ?? item.FechaCreacion) < period.FechaHastaExclusiva);

        if (!query.IncluirNoPublicadas)
        {
            resenasQuery = resenasQuery.Where(IsApprovedPublicExpression());
        }

        var resenas = await resenasQuery.ToArrayAsync(cancellationToken);

        return ServiceResult<ReputacionNegocioDto>.Success(BuildReputacion(
            idNegocio,
            negocio,
            period.FechaDesde,
            period.FechaHasta,
            query.IncluirNoPublicadas,
            resenas));
    }

    public async Task<ServiceResult<IReadOnlyCollection<ResenaEstadoSelectDto>>> GetEstadosSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(currentUser, idNegocio, cancellationToken);
        if (!access.Exists)
        {
            return ServiceResult<IReadOnlyCollection<ResenaEstadoSelectDto>>.NotFound("El negocio no existe.");
        }

        if (!access.CanView)
        {
            return ServiceResult<IReadOnlyCollection<ResenaEstadoSelectDto>>.Forbidden("No tienes acceso para ver filtros de reseñas de este negocio.");
        }

        var counts = await ApplyBusinessScope(BaseQuery(idNegocio).AsNoTracking(), access)
            .Where(item => item.Activo)
            .GroupBy(item => item.Estado)
            .Select(group => new { Estado = group.Key, Cantidad = group.Count() })
            .ToArrayAsync(cancellationToken);

        var result = EstadosResena
            .Select(estado => new ResenaEstadoSelectDto(
                estado,
                estado,
                counts.FirstOrDefault(item => item.Estado == estado)?.Cantidad ?? 0))
            .ToArray();

        return ServiceResult<IReadOnlyCollection<ResenaEstadoSelectDto>>.Success(result);
    }

    public async Task<ServiceResult<IReadOnlyCollection<ResenaPuntuacionSelectDto>>> GetPuntuacionesSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(currentUser, idNegocio, cancellationToken);
        if (!access.Exists)
        {
            return ServiceResult<IReadOnlyCollection<ResenaPuntuacionSelectDto>>.NotFound("El negocio no existe.");
        }

        if (!access.CanView)
        {
            return ServiceResult<IReadOnlyCollection<ResenaPuntuacionSelectDto>>.Forbidden("No tienes acceso para ver filtros de reseñas de este negocio.");
        }

        var counts = await ApplyBusinessScope(BaseQuery(idNegocio).AsNoTracking(), access)
            .Where(item => item.Activo)
            .GroupBy(item => item.Puntuacion)
            .Select(group => new { Puntuacion = group.Key, Cantidad = group.Count() })
            .ToArrayAsync(cancellationToken);

        var result = Enumerable.Range(1, 5)
            .Select(puntuacion => new ResenaPuntuacionSelectDto(
                (byte)puntuacion,
                $"{puntuacion} estrella{(puntuacion == 1 ? string.Empty : "s")}",
                counts.FirstOrDefault(item => item.Puntuacion == puntuacion)?.Cantidad ?? 0))
            .ToArray();

        return ServiceResult<IReadOnlyCollection<ResenaPuntuacionSelectDto>>.Success(result);
    }

    public async Task<PagedResult<ResenaPublicaDto>> GetPublicasAsync(
        string slug,
        ResenaPublicaQuery query,
        CancellationToken cancellationToken)
    {
        var negocio = await dbContext.Negocios
            .AsNoTracking()
            .Where(item => item.Slug == slug && item.Activo)
            .Select(item => new { item.IdNegocio })
            .FirstOrDefaultAsync(cancellationToken);

        if (negocio is null)
        {
            return new PagedResult<ResenaPublicaDto>([], query.PageNumber, query.PageSize, 0);
        }

        var configuracion = await GetConfiguracionOrDefaultAsync(negocio.IdNegocio, cancellationToken);
        if (!configuracion.MostrarResenasPublicas)
        {
            return new PagedResult<ResenaPublicaDto>([], query.PageNumber, query.PageSize, 0);
        }

        var resenasQuery = BaseQuery(negocio.IdNegocio)
            .AsNoTracking()
            .Where(IsApprovedPublicExpression());

        if (query.Puntuacion.HasValue)
        {
            resenasQuery = resenasQuery.Where(item => item.Puntuacion == query.Puntuacion.Value);
        }

        if (query.IdServicio.HasValue)
        {
            resenasQuery = resenasQuery.Where(item => item.IdServicio == query.IdServicio.Value);
        }

        if (query.IdPrestador.HasValue)
        {
            resenasQuery = resenasQuery.Where(item => item.IdPrestador == query.IdPrestador.Value);
        }

        var totalItems = await resenasQuery.CountAsync(cancellationToken);
        var items = await resenasQuery
            .OrderByDescending(item => item.FechaPublicacion ?? item.FechaCreacion)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<ResenaPublicaDto>(
            items.Select(ToPublicDto).ToArray(),
            query.PageNumber,
            query.PageSize,
            totalItems);
    }

    public async Task<ServiceResult<ResenaResumenDto>> GetResumenPublicoAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var negocio = await dbContext.Negocios
            .AsNoTracking()
            .Where(item => item.Slug == slug && item.Activo)
            .Select(item => new { item.IdNegocio, item.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<ResenaResumenDto>.NotFound("El negocio no existe.");
        }

        var configuracion = await GetConfiguracionOrDefaultAsync(negocio.IdNegocio, cancellationToken);
        if (!configuracion.MostrarResenasPublicas)
        {
            return ServiceResult<ResenaResumenDto>.Success(BuildResumen(negocio.IdNegocio, negocio.Nombre, []));
        }

        var resenas = await BaseQuery(negocio.IdNegocio)
            .AsNoTracking()
            .Where(IsApprovedPublicExpression())
            .ToArrayAsync(cancellationToken);

        return ServiceResult<ResenaResumenDto>.Success(BuildResumen(negocio.IdNegocio, negocio.Nombre, resenas));
    }

    public async Task<ServiceResult<ConfiguracionResenaNegocioDto>> GetConfiguracionAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<ConfiguracionResenaNegocioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageConfigurationAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<ConfiguracionResenaNegocioDto>.Forbidden("No tienes acceso para ver la configuración de reseñas de este negocio.");
        }

        var configuracion = await GetOrCreateConfiguracionAsync(idNegocio, cancellationToken);
        return ServiceResult<ConfiguracionResenaNegocioDto>.Success(ToDto(configuracion));
    }

    public async Task<ServiceResult<ConfiguracionResenaNegocioDto>> UpdateConfiguracionAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        UpdateConfiguracionResenaNegocioRequest request,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<ConfiguracionResenaNegocioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageConfigurationAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<ConfiguracionResenaNegocioDto>.Forbidden("No tienes acceso para editar la configuración de reseñas de este negocio.");
        }

        var configuracion = await GetOrCreateConfiguracionAsync(idNegocio, cancellationToken);
        var previous = ToAuditSnapshot(configuracion);

        configuracion.ResenasActivas = request.ResenasActivas;
        configuracion.AutoaprobarResenas = request.AutoaprobarResenas;
        configuracion.DiasMaximosParaCalificar = request.DiasMaximosParaCalificar;
        configuracion.PuntuacionMaximaAlertaOperativa = request.PuntuacionMaximaAlertaOperativa;
        configuracion.PermitirRespuestaNegocio = request.PermitirRespuestaNegocio;
        configuracion.MostrarResenasPublicas = request.MostrarResenasPublicas;
        configuracion.FechaActualizacion = DateTime.Now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Resenas",
                "Configurar",
                nameof(ConfiguracionResenaNegocio),
                configuracion.IdConfiguracionResenaNegocio.ToString(),
                $"Configuración de reseñas actualizada para {configuracion.Negocio.Nombre}",
                previous,
                ToAuditSnapshot(configuracion)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ConfiguracionResenaNegocioDto>.Success(ToDto(configuracion));
    }

    public async Task<ServiceResult<ResenaNegocioDto>> GetMiResenaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<ResenaNegocioDto>.Forbidden("Debes iniciar sesión para ver tu reseña.");
        }

        var resena = await BaseQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.IdCita == idCita &&
                item.Cita.Cliente.UserId == currentUser.UserId &&
                item.Activo,
                cancellationToken);

        return resena is null
            ? ServiceResult<ResenaNegocioDto>.NotFound("La reseña no existe para esta cita.")
            : ServiceResult<ResenaNegocioDto>.Success(ToDto(resena));
    }

    public async Task<ServiceResult<ResenaNegocioDto>> CreateMiResenaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CrearResenaRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<ResenaNegocioDto>.Forbidden("Debes iniciar sesión para calificar una cita.");
        }

        var cita = await BaseCitaQuery()
            .FirstOrDefaultAsync(item => item.IdCita == idCita && item.Cliente.UserId == currentUser.UserId, cancellationToken);

        if (cita is null)
        {
            return ServiceResult<ResenaNegocioDto>.NotFound("La cita no existe o no pertenece al usuario autenticado.");
        }

        var configuracion = await GetConfiguracionOrDefaultAsync(cita.IdNegocio, cancellationToken);
        var validation = await ValidateCanCreateAsync(cita, configuracion, cancellationToken);
        if (validation.Count > 0)
        {
            return ServiceResult<ResenaNegocioDto>.Validation(validation);
        }

        var resena = CreateFromCita(cita, currentUser.UserId, request.Puntuacion, request.Comentario, configuracion);
        dbContext.ResenasNegocio.Add(resena);
        await MarkSolicitudesUsadasAsync(cita.IdNegocio, cita.IdCita, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await CrearNotificacionesNegocioPorResenaAsync(resena, cita, cancellationToken);
        await RegistrarAuditoriaAsync(currentUser, resena, "Crear", $"Reseña creada para la cita {cita.Codigo}", null, ToAuditSnapshot(resena), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(cita.IdNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdResenaNegocio == resena.IdResenaNegocio, cancellationToken);

        return ServiceResult<ResenaNegocioDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<ResenaNegocioDto>> UpdateMiResenaAsync(
        CurrentUserContext currentUser,
        int idCita,
        ActualizarResenaRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<ResenaNegocioDto>.Forbidden("Debes iniciar sesión para editar tu reseña.");
        }

        var resena = await BaseQuery()
            .FirstOrDefaultAsync(item =>
                item.IdCita == idCita &&
                item.Cita.Cliente.UserId == currentUser.UserId &&
                item.Activo,
                cancellationToken);

        if (resena is null)
        {
            return ServiceResult<ResenaNegocioDto>.NotFound("La reseña no existe o no pertenece al usuario autenticado.");
        }

        var previous = ToAuditSnapshot(resena);
        var configuracion = await GetConfiguracionOrDefaultAsync(resena.IdNegocio, cancellationToken);
        if (!configuracion.ResenasActivas)
        {
            return ServiceResult<ResenaNegocioDto>.Validation([
                new ValidationError(string.Empty, "El negocio no tiene habilitada la edición de reseñas.")
            ]);
        }

        var fechaLimite = resena.Cita.FechaFin.AddDays(configuracion.DiasMaximosParaCalificar);
        if (DateTime.Now > fechaLimite)
        {
            return ServiceResult<ResenaNegocioDto>.Validation([
                new ValidationError(string.Empty, $"La reseña solo puede editarse hasta {configuracion.DiasMaximosParaCalificar} días después de la atención.")
            ]);
        }

        resena.Puntuacion = request.Puntuacion;
        resena.Comentario = TrimOptional(request.Comentario);
        ApplyEstadoInicial(resena, configuracion);
        ApplyAlertaOperativa(resena, configuracion);
        resena.FechaActualizacion = DateTime.Now;
        if (resena.Estado == ResenaNegocioEstados.Pendiente)
        {
            resena.FechaPublicacion = null;
            resena.ModeradoPorUserId = null;
            resena.FechaModeracion = null;
            resena.MotivoModeracion = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaAsync(currentUser, resena, "Actualizar", $"Reseña actualizada para la cita {resena.Cita.Codigo}", previous, ToAuditSnapshot(resena), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ResenaNegocioDto>.Success(ToDto(resena));
    }

    public async Task<ServiceResult<SolicitudResenaPreviewDto>> ValidarSolicitudAsync(
        ValidarSolicitudResenaRequest request,
        CancellationToken cancellationToken)
    {
        var solicitud = await FindSolicitudByTokenAsync(request.Token, tracking: true, cancellationToken);
        if (solicitud is null)
        {
            return ServiceResult<SolicitudResenaPreviewDto>.Success(InvalidPreview("No encontramos una solicitud de reseña válida."));
        }

        var invalid = await ValidateSolicitudStateAsync(solicitud, saveChanges: true, cancellationToken);
        if (invalid is not null)
        {
            return ServiceResult<SolicitudResenaPreviewDto>.Success(invalid);
        }

        return ServiceResult<SolicitudResenaPreviewDto>.Success(new SolicitudResenaPreviewDto(
            true,
            solicitud.Estado,
            solicitud.Negocio.Nombre,
            solicitud.Cliente.Nombre,
            solicitud.Cita.Servicio.Nombre,
            solicitud.Cita.Prestador?.Nombre,
            solicitud.Cita.FechaFin,
            solicitud.FechaExpiracion,
            null));
    }

    public async Task<ServiceResult<ResenaPublicaCreadaDto>> CreatePublicaAsync(
        CrearResenaPublicaRequest request,
        CancellationToken cancellationToken)
    {
        var solicitud = await FindSolicitudByTokenAsync(request.Token, tracking: true, cancellationToken);
        if (solicitud is null)
        {
            return ServiceResult<ResenaPublicaCreadaDto>.Validation([
                new ValidationError(nameof(CrearResenaPublicaRequest.Token), "La solicitud de reseña no existe o el token no es válido.")
            ]);
        }

        var invalid = await ValidateSolicitudStateAsync(solicitud, saveChanges: true, cancellationToken);
        if (invalid is not null)
        {
            return ServiceResult<ResenaPublicaCreadaDto>.Validation([
                new ValidationError(nameof(CrearResenaPublicaRequest.Token), invalid.Mensaje ?? "La solicitud de reseña no está disponible.")
            ]);
        }

        var configuracion = await GetConfiguracionOrDefaultAsync(solicitud.IdNegocio, cancellationToken);
        var validation = await ValidateCanCreateAsync(solicitud.Cita, configuracion, cancellationToken);
        if (validation.Count > 0)
        {
            return ServiceResult<ResenaPublicaCreadaDto>.Validation(validation);
        }

        var resena = CreateFromCita(solicitud.Cita, solicitud.Cita.Cliente.UserId, request.Puntuacion, request.Comentario, configuracion);
        dbContext.ResenasNegocio.Add(resena);
        solicitud.Estado = SolicitudResenaEstados.Usada;
        solicitud.FechaUso = DateTime.Now;
        await dbContext.SaveChangesAsync(cancellationToken);

        await CrearNotificacionesNegocioPorResenaAsync(resena, solicitud.Cita, cancellationToken);
        await RegistrarAuditoriaAsync(new CurrentUserContext(resena.UserId ?? string.Empty, []), resena, "CrearPublica", $"Reseña pública creada para la cita {solicitud.Cita.Codigo}", null, ToAuditSnapshot(resena), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ResenaPublicaCreadaDto>.Success(new ResenaPublicaCreadaDto(
            resena.IdResenaNegocio,
            resena.Estado,
            resena.Estado == ResenaNegocioEstados.Pendiente,
            resena.Estado == ResenaNegocioEstados.Pendiente
                ? "Gracias por tu opinión. La reseña quedará visible cuando el negocio la revise."
                : "Gracias por tu opinión. La reseña fue publicada correctamente."));
    }

    public Task<ServiceResult<ResenaNegocioDto>> AprobarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idResena,
        ModerarResenaRequest request,
        CancellationToken cancellationToken)
    {
        return ModerarAsync(currentUser, idNegocio, idResena, ResenaNegocioEstados.Aprobada, request.Motivo, cancellationToken);
    }

    public Task<ServiceResult<ResenaNegocioDto>> RechazarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idResena,
        ModerarResenaRequest request,
        CancellationToken cancellationToken)
    {
        return ModerarAsync(currentUser, idNegocio, idResena, ResenaNegocioEstados.Rechazada, request.Motivo, cancellationToken);
    }

    public Task<ServiceResult<ResenaNegocioDto>> OcultarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idResena,
        ModerarResenaRequest request,
        CancellationToken cancellationToken)
    {
        return ModerarAsync(currentUser, idNegocio, idResena, ResenaNegocioEstados.Oculta, request.Motivo, cancellationToken);
    }

    public async Task<ServiceResult<ResenaNegocioDto>> ResponderAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idResena,
        ResponderResenaRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanModerateAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<ResenaNegocioDto>.Forbidden("No tienes acceso para responder reseñas de este negocio.");
        }

        var resena = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdResenaNegocio == idResena && item.Activo, cancellationToken);

        if (resena is null)
        {
            return ServiceResult<ResenaNegocioDto>.NotFound("La reseña no existe.");
        }

        if (resena.Estado == ResenaNegocioEstados.Rechazada)
        {
            return ServiceResult<ResenaNegocioDto>.Validation([
                new ValidationError(string.Empty, "No se puede responder una reseña rechazada.")
            ]);
        }

        var configuracion = await GetConfiguracionOrDefaultAsync(idNegocio, cancellationToken);
        if (!configuracion.PermitirRespuestaNegocio)
        {
            return ServiceResult<ResenaNegocioDto>.Validation([
                new ValidationError(string.Empty, "El negocio no tiene habilitadas las respuestas a reseñas.")
            ]);
        }

        var previous = ToAuditSnapshot(resena);
        resena.RespuestaNegocio = TrimRequired(request.Respuesta);
        resena.RespondidoPorUserId = currentUser.UserId;
        resena.FechaRespuesta = DateTime.Now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaAsync(currentUser, resena, "Responder", $"Respuesta registrada para la reseña {resena.IdResenaNegocio}", previous, ToAuditSnapshot(resena), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ResenaNegocioDto>.Success(ToDto(resena));
    }

    public async Task<ServiceResult<CrearSolicitudResenaResultDto>> CrearSolicitudPostAtencionAsync(
        int idCita,
        CancellationToken cancellationToken)
    {
        var cita = await BaseCitaQuery()
            .FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);

        if (cita is null ||
            !cita.EstadoCita.Nombre.Equals(AtendidaStateName, StringComparison.OrdinalIgnoreCase) ||
            await dbContext.ResenasNegocio.AnyAsync(item => item.IdNegocio == cita.IdNegocio && item.IdCita == cita.IdCita, cancellationToken))
        {
            return ServiceResult<CrearSolicitudResenaResultDto>.Success(new CrearSolicitudResenaResultDto(false, null, null));
        }

        var configuracion = await GetConfiguracionOrDefaultAsync(cita.IdNegocio, cancellationToken);
        if (!configuracion.ResenasActivas)
        {
            return ServiceResult<CrearSolicitudResenaResultDto>.Success(new CrearSolicitudResenaResultDto(false, null, null));
        }

        var fechaExpiracion = cita.FechaFin.AddDays(configuracion.DiasMaximosParaCalificar);
        if (fechaExpiracion <= DateTime.Now)
        {
            return ServiceResult<CrearSolicitudResenaResultDto>.Success(new CrearSolicitudResenaResultDto(false, null, null));
        }

        var destinatario = await ResolveClienteEmailAsync(cita.Cliente, cancellationToken);
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return ServiceResult<CrearSolicitudResenaResultDto>.Success(new CrearSolicitudResenaResultDto(false, null, null));
        }

        await MarkExpiredSolicitudesAsync(cita.IdNegocio, cita.IdCita, cancellationToken);

        var activePending = await dbContext.SolicitudesResena
            .AnyAsync(item =>
                item.IdNegocio == cita.IdNegocio &&
                item.IdCita == cita.IdCita &&
                item.Estado == SolicitudResenaEstados.Pendiente &&
                item.FechaExpiracion > DateTime.Now,
                cancellationToken);

        if (activePending)
        {
            return ServiceResult<CrearSolicitudResenaResultDto>.Success(new CrearSolicitudResenaResultDto(false, null, destinatario));
        }

        var token = InvitationTokenGenerator.Generate();
        var solicitud = new SolicitudResena
        {
            IdNegocio = cita.IdNegocio,
            IdCita = cita.IdCita,
            IdCliente = cita.IdCliente,
            Email = destinatario.Trim(),
            NormalizedEmail = NormalizeEmail(destinatario),
            TokenHash = TokenHasher.Hash(token),
            Estado = SolicitudResenaEstados.Pendiente,
            FechaCreacion = DateTime.Now,
            FechaExpiracion = fechaExpiracion
        };

        dbContext.SolicitudesResena.Add(solicitud);
        await CrearNotificacionPostAtencionAsync(cita, destinatario, BuildResenaLink(token), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CrearSolicitudResenaResultDto>.Success(
            new CrearSolicitudResenaResultDto(true, solicitud.IdSolicitudResena, destinatario));
    }

    public async Task<ServiceResult<ExpirarSolicitudesResenaResultDto>> ExpirarSolicitudesPendientesAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var solicitudes = await dbContext.SolicitudesResena
            .Where(item =>
                item.Estado == SolicitudResenaEstados.Pendiente &&
                item.FechaExpiracion <= now)
            .ToArrayAsync(cancellationToken);

        foreach (var solicitud in solicitudes)
        {
            solicitud.Estado = SolicitudResenaEstados.Expirada;
        }

        if (solicitudes.Length > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<ExpirarSolicitudesResenaResultDto>.Success(
            new ExpirarSolicitudesResenaResultDto(solicitudes.Length));
    }

    public async Task CancelarSolicitudesPendientesCitaAsync(
        int idCita,
        CancellationToken cancellationToken)
    {
        var solicitudes = await dbContext.SolicitudesResena
            .Where(item =>
                item.IdCita == idCita &&
                item.Estado == SolicitudResenaEstados.Pendiente)
            .ToArrayAsync(cancellationToken);

        foreach (var solicitud in solicitudes)
        {
            solicitud.Estado = SolicitudResenaEstados.Cancelada;
            solicitud.FechaCancelacion = DateTime.Now;
        }
    }

    private async Task<ServiceResult<ResenaNegocioDto>> ModerarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idResena,
        string estado,
        string? motivo,
        CancellationToken cancellationToken)
    {
        if (!await CanModerateAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<ResenaNegocioDto>.Forbidden("No tienes acceso para moderar reseñas de este negocio.");
        }

        var resena = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdResenaNegocio == idResena && item.Activo, cancellationToken);

        if (resena is null)
        {
            return ServiceResult<ResenaNegocioDto>.NotFound("La reseña no existe.");
        }

        var previous = ToAuditSnapshot(resena);
        var now = DateTime.Now;
        resena.Estado = estado;
        resena.EsVisiblePublicamente = estado == ResenaNegocioEstados.Aprobada;
        resena.FechaPublicacion = estado == ResenaNegocioEstados.Aprobada ? now : null;
        resena.ModeradoPorUserId = currentUser.UserId;
        resena.FechaModeracion = now;
        resena.MotivoModeracion = TrimOptional(motivo);
        resena.FechaActualizacion = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaAsync(currentUser, resena, estado, $"Reseña marcada como {estado}", previous, ToAuditSnapshot(resena), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ResenaNegocioDto>.Success(ToDto(resena));
    }

    private IQueryable<ResenaNegocio> BaseQuery(int idNegocio)
    {
        return BaseQuery().Where(item => item.IdNegocio == idNegocio);
    }

    private IQueryable<ResenaNegocio> BaseQuery()
    {
        return dbContext.ResenasNegocio
            .Include(item => item.Negocio)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.EstadoCita)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.Cliente)
            .Include(item => item.Cliente)
            .Include(item => item.Servicio)
            .Include(item => item.Prestador);
    }

    private IQueryable<Cita> BaseCitaQuery()
    {
        return dbContext.Citas
            .Include(item => item.Negocio)
            .Include(item => item.Cliente)
                .ThenInclude(cliente => cliente.Usuario)
            .Include(item => item.Servicio)
            .Include(item => item.Prestador)
            .Include(item => item.EstadoCita);
    }

    private async Task<SolicitudResena?> FindSolicitudByTokenAsync(
        string token,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(token.Trim());
        var query = dbContext.SolicitudesResena
            .Include(item => item.Negocio)
            .Include(item => item.Cliente)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.Negocio)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.Cliente)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.EstadoCita)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.Servicio)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.Prestador)
            .Where(item => item.TokenHash == tokenHash);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<ValidationError>> ValidateCanCreateAsync(
        Cita cita,
        ConfiguracionResenaNegocio configuracion,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (!configuracion.ResenasActivas)
        {
            errors.Add(new ValidationError(string.Empty, "El negocio no tiene habilitadas las reseñas."));
        }

        if (!cita.EstadoCita.Nombre.Equals(AtendidaStateName, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new ValidationError(string.Empty, "Solo se puede calificar una cita atendida."));
        }

        var fechaLimite = cita.FechaFin.AddDays(configuracion.DiasMaximosParaCalificar);
        if (DateTime.Now > fechaLimite)
        {
            errors.Add(new ValidationError(string.Empty, $"La cita solo puede calificarse hasta {configuracion.DiasMaximosParaCalificar} días después de la atención."));
        }

        var alreadyExists = await dbContext.ResenasNegocio
            .AnyAsync(item => item.IdNegocio == cita.IdNegocio && item.IdCita == cita.IdCita, cancellationToken);

        if (alreadyExists)
        {
            errors.Add(new ValidationError(string.Empty, "Esta cita ya tiene una reseña registrada."));
        }

        return errors;
    }

    private async Task<SolicitudResenaPreviewDto?> ValidateSolicitudStateAsync(
        SolicitudResena solicitud,
        bool saveChanges,
        CancellationToken cancellationToken)
    {
        if (solicitud.Estado != SolicitudResenaEstados.Pendiente)
        {
            return InvalidPreview($"La solicitud de reseña está en estado {solicitud.Estado}.", solicitud);
        }

        var configuracion = await GetConfiguracionOrDefaultAsync(solicitud.IdNegocio, cancellationToken);
        if (!configuracion.ResenasActivas)
        {
            return InvalidPreview("El negocio no tiene habilitadas las reseñas.", solicitud);
        }

        if (solicitud.FechaExpiracion <= DateTime.Now)
        {
            solicitud.Estado = SolicitudResenaEstados.Expirada;
            if (saveChanges)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return InvalidPreview("La solicitud de reseña expiró.", solicitud);
        }

        var alreadyExists = await dbContext.ResenasNegocio
            .AnyAsync(item => item.IdNegocio == solicitud.IdNegocio && item.IdCita == solicitud.IdCita, cancellationToken);

        if (alreadyExists)
        {
            return InvalidPreview("Esta cita ya tiene una reseña registrada.", solicitud);
        }

        if (!solicitud.Cita.EstadoCita.Nombre.Equals(AtendidaStateName, StringComparison.OrdinalIgnoreCase))
        {
            return InvalidPreview("La cita aún no está marcada como atendida.", solicitud);
        }

        var fechaLimite = solicitud.Cita.FechaFin.AddDays(configuracion.DiasMaximosParaCalificar);
        if (DateTime.Now > fechaLimite)
        {
            return InvalidPreview("La solicitud de reseña expiró por las reglas del negocio.", solicitud);
        }

        return null;
    }

    private async Task MarkExpiredSolicitudesAsync(int idNegocio, int idCita, CancellationToken cancellationToken)
    {
        var expired = await dbContext.SolicitudesResena
            .Where(item =>
                item.IdNegocio == idNegocio &&
                item.IdCita == idCita &&
                item.Estado == SolicitudResenaEstados.Pendiente &&
                item.FechaExpiracion <= DateTime.Now)
            .ToArrayAsync(cancellationToken);

        foreach (var item in expired)
        {
            item.Estado = SolicitudResenaEstados.Expirada;
        }
    }

    private async Task MarkSolicitudesUsadasAsync(int idNegocio, int idCita, CancellationToken cancellationToken)
    {
        var solicitudes = await dbContext.SolicitudesResena
            .Where(item =>
                item.IdNegocio == idNegocio &&
                item.IdCita == idCita &&
                item.Estado == SolicitudResenaEstados.Pendiente)
            .ToArrayAsync(cancellationToken);

        foreach (var solicitud in solicitudes)
        {
            solicitud.Estado = SolicitudResenaEstados.Usada;
            solicitud.FechaUso = DateTime.Now;
        }
    }

    private async Task CrearNotificacionPostAtencionAsync(
        Cita cita,
        string destinatario,
        string link,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposNotificacion
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Nombre == TipoPostAtencionName && item.Activo, cancellationToken);
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

        dbContext.Notificaciones.Add(new Notificacion
        {
            IdNegocio = cita.IdNegocio,
            IdCita = cita.IdCita,
            IdTipoNotificacion = tipo.IdTipoNotificacion,
            IdCanalNotificacion = canal.IdCanalNotificacion,
            IdEstadoNotificacion = estado.IdEstadoNotificacion,
            Destinatario = destinatario.Trim(),
            Asunto = $"Califica tu atención en {cita.Negocio.Nombre}",
            Mensaje = link,
            FechaProgramada = DateTime.Now
        });
    }

    private async Task CrearNotificacionesNegocioPorResenaAsync(
        ResenaNegocio resena,
        Cita cita,
        CancellationToken cancellationToken)
    {
        var tipoNombre = resena.EsAlertaOperativa
            ? TipoAlertaResenaNegocioName
            : TipoNuevaResenaNegocioName;
        var metadata = await GetNotificationMetadataAsync(tipoNombre, cancellationToken);

        if (metadata is null)
        {
            return;
        }

        var emails = await dbContext.NegocioUsuarios
            .AsNoTracking()
            .Where(item =>
                item.IdNegocio == cita.IdNegocio &&
                item.Activo &&
                item.Usuario.Email != null &&
                (item.RolNegocio.Nombre == OwnerRoleName || item.RolNegocio.Nombre == AdminRoleName))
            .Select(item => item.Usuario.Email!)
            .ToArrayAsync(cancellationToken);

        var destinatarios = emails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Where(email => email.Length <= 200)
            .GroupBy(NormalizeEmail)
            .Select(group => group.First())
            .ToArray();

        foreach (var destinatario in destinatarios)
        {
            dbContext.Notificaciones.Add(new Notificacion
            {
                IdNegocio = cita.IdNegocio,
                IdCita = cita.IdCita,
                IdResenaNegocio = resena.IdResenaNegocio,
                IdTipoNotificacion = metadata.Value.IdTipoNotificacion,
                IdCanalNotificacion = metadata.Value.IdCanalNotificacion,
                IdEstadoNotificacion = metadata.Value.IdEstadoNotificacion,
                Destinatario = destinatario,
                Asunto = GetAsuntoNotificacionResena(cita, resena),
                Mensaje = GetMensajeNotificacionResena(cita, resena),
                FechaProgramada = DateTime.Now
            });
        }
    }

    private async Task<NotificationMetadata?> GetNotificationMetadataAsync(
        string tipoNombre,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposNotificacion
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Nombre == tipoNombre && item.Activo, cancellationToken);
        var canal = await dbContext.CanalesNotificacion
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Nombre == CanalEmailName && item.Activo, cancellationToken);
        var estado = await dbContext.EstadosNotificacion
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Nombre == EstadoNotificacionPendienteName && item.Activo, cancellationToken);

        return tipo is null || canal is null || estado is null
            ? null
            : new NotificationMetadata(tipo.IdTipoNotificacion, canal.IdCanalNotificacion, estado.IdEstadoNotificacion);
    }

    private static string GetAsuntoNotificacionResena(Cita cita, ResenaNegocio resena)
    {
        return resena.EsAlertaOperativa
            ? $"Alerta operativa: reseña baja en {cita.Negocio.Nombre}"
            : $"Nueva reseña recibida en {cita.Negocio.Nombre}";
    }

    private static string GetMensajeNotificacionResena(Cita cita, ResenaNegocio resena)
    {
        var comentario = string.IsNullOrWhiteSpace(resena.Comentario)
            ? "Sin comentario."
            : resena.Comentario.Trim();

        return string.Join(
            Environment.NewLine,
            resena.EsAlertaOperativa ? "Alerta operativa por baja puntuación" : "Nueva reseña recibida",
            string.Empty,
            $"Negocio: {cita.Negocio.Nombre}",
            $"Código cita: {cita.Codigo}",
            $"Cliente: {resena.ClienteNombreSnapshot}",
            $"Servicio: {resena.ServicioNombreSnapshot}",
            $"Prestador: {resena.PrestadorNombreSnapshot ?? "Sin prestador"}",
            $"Puntuación: {resena.Puntuacion}/5",
            $"Estado: {resena.Estado}",
            $"Alerta operativa: {(resena.EsAlertaOperativa ? "Sí" : "No")}",
            $"Comentario: {comentario}",
            string.Empty,
            resena.EsAlertaOperativa
                ? "Revisa esta experiencia con prioridad y coordina una acción interna si corresponde."
                : "Revisa la reseña desde el panel del negocio.");
    }

    private async Task<string?> ResolveClienteEmailAsync(Cliente cliente, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(cliente.Email))
        {
            return cliente.Email.Trim();
        }

        if (string.IsNullOrWhiteSpace(cliente.UserId))
        {
            return null;
        }

        var user = cliente.Usuario ?? await userManager.FindByIdAsync(cliente.UserId);
        return string.IsNullOrWhiteSpace(user?.Email) ? null : user.Email.Trim();
    }

    private async Task<ConfiguracionResenaNegocio> GetOrCreateConfiguracionAsync(
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var configuracion = await dbContext.ConfiguracionesResenaNegocio
            .Include(item => item.Negocio)
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio, cancellationToken);

        if (configuracion is not null)
        {
            return configuracion;
        }

        var negocio = await dbContext.Negocios.FirstAsync(item => item.IdNegocio == idNegocio, cancellationToken);
        configuracion = new ConfiguracionResenaNegocio
        {
            IdNegocio = idNegocio,
            Negocio = negocio,
            ResenasActivas = true,
            AutoaprobarResenas = false,
            DiasMaximosParaCalificar = 15,
            PuntuacionMaximaAlertaOperativa = 2,
            PermitirRespuestaNegocio = true,
            MostrarResenasPublicas = true,
            FechaCreacion = DateTime.Now,
            FechaActualizacion = DateTime.Now
        };

        dbContext.ConfiguracionesResenaNegocio.Add(configuracion);
        await dbContext.SaveChangesAsync(cancellationToken);

        return configuracion;
    }

    private async Task<ConfiguracionResenaNegocio> GetConfiguracionOrDefaultAsync(
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var configuracion = await dbContext.ConfiguracionesResenaNegocio
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio, cancellationToken);

        return configuracion ?? new ConfiguracionResenaNegocio
        {
            IdNegocio = idNegocio,
            ResenasActivas = true,
            AutoaprobarResenas = false,
            DiasMaximosParaCalificar = 15,
            PuntuacionMaximaAlertaOperativa = 2,
            PermitirRespuestaNegocio = true,
            MostrarResenasPublicas = true,
            FechaCreacion = DateTime.Now,
            FechaActualizacion = DateTime.Now
        };
    }

    private async Task<bool> NegocioExistsAsync(int idNegocio, CancellationToken cancellationToken)
    {
        return await dbContext.Negocios.AnyAsync(item => item.IdNegocio == idNegocio, cancellationToken);
    }

    private async Task<ResenaAccess> GetAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Negocios.AnyAsync(item => item.IdNegocio == idNegocio, cancellationToken);
        if (!exists)
        {
            return new ResenaAccess(false, false, []);
        }

        if (currentUser.IsSuperAdmin)
        {
            return new ResenaAccess(true, true, []);
        }

        if (!currentUser.IsAuthenticated)
        {
            return new ResenaAccess(true, false, []);
        }

        var canViewAll = await dbContext.NegocioUsuarios.AnyAsync(
            item =>
                item.IdNegocio == idNegocio &&
                item.UserId == currentUser.UserId &&
                item.Activo &&
                (item.RolNegocio.Nombre == OwnerRoleName ||
                    item.RolNegocio.Nombre == AdminRoleName ||
                    item.RolNegocio.Nombre == RecepcionistaRoleName),
            cancellationToken);

        if (canViewAll)
        {
            return new ResenaAccess(true, true, []);
        }

        var prestadorIds = await dbContext.Prestadores
            .AsNoTracking()
            .Where(item => item.IdNegocio == idNegocio && item.UserId == currentUser.UserId && item.Activo)
            .Select(item => item.IdPrestador)
            .ToArrayAsync(cancellationToken);

        return new ResenaAccess(true, prestadorIds.Length > 0, prestadorIds);
    }

    private async Task<bool> CanModerateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        return await CanManageConfigurationAsync(currentUser, idNegocio, cancellationToken);
    }

    private async Task<bool> CanManageConfigurationAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        if (!currentUser.IsAuthenticated)
        {
            return false;
        }

        return await dbContext.NegocioUsuarios.AnyAsync(
            item =>
                item.IdNegocio == idNegocio &&
                item.UserId == currentUser.UserId &&
                item.Activo &&
                (item.RolNegocio.Nombre == OwnerRoleName ||
                    item.RolNegocio.Nombre == AdminRoleName),
            cancellationToken);
    }

    private static IQueryable<ResenaNegocio> ApplyBusinessScope(IQueryable<ResenaNegocio> query, ResenaAccess access)
    {
        if (access.CanViewAll)
        {
            return query;
        }

        return query.Where(item => item.IdPrestador.HasValue && access.PrestadorIds.Contains(item.IdPrestador.Value));
    }

    private static IQueryable<ResenaNegocio> ApplyFilters(IQueryable<ResenaNegocio> query, ResenaNegocioQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(item =>
                item.Cita.Codigo.Contains(search) ||
                item.ClienteNombreSnapshot.Contains(search) ||
                item.ServicioNombreSnapshot.Contains(search) ||
                (item.PrestadorNombreSnapshot != null && item.PrestadorNombreSnapshot.Contains(search)) ||
                (item.Comentario != null && item.Comentario.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filters.Estado))
        {
            var estado = filters.Estado.Trim();
            query = query.Where(item => item.Estado == estado);
        }

        if (filters.Puntuacion.HasValue)
        {
            query = query.Where(item => item.Puntuacion == filters.Puntuacion.Value);
        }

        if (filters.IdServicio.HasValue)
        {
            query = query.Where(item => item.IdServicio == filters.IdServicio.Value);
        }

        if (filters.IdPrestador.HasValue)
        {
            query = query.Where(item => item.IdPrestador == filters.IdPrestador.Value);
        }

        if (filters.FechaDesde.HasValue)
        {
            query = query.Where(item => item.FechaCreacion >= filters.FechaDesde.Value);
        }

        if (filters.FechaHasta.HasValue)
        {
            query = query.Where(item => item.FechaCreacion <= filters.FechaHasta.Value);
        }

        if (filters.SoloVisiblesPublicamente.HasValue)
        {
            query = query.Where(item => item.EsVisiblePublicamente == filters.SoloVisiblesPublicamente.Value);
        }

        if (filters.SoloAlertasOperativas.HasValue)
        {
            query = query.Where(item => item.EsAlertaOperativa == filters.SoloAlertasOperativas.Value);
        }

        return query;
    }

    private static System.Linq.Expressions.Expression<Func<ResenaNegocio, bool>> IsApprovedPublicExpression()
    {
        return item =>
            item.Activo &&
            item.Estado == ResenaNegocioEstados.Aprobada &&
            item.EsVisiblePublicamente;
    }

    private static ResenaNegocio CreateFromCita(
        Cita cita,
        string? userId,
        byte puntuacion,
        string? comentario,
        ConfiguracionResenaNegocio configuracion)
    {
        var resena = new ResenaNegocio
        {
            IdNegocio = cita.IdNegocio,
            IdCita = cita.IdCita,
            IdCliente = cita.IdCliente,
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
            IdServicio = cita.IdServicio,
            IdPrestador = cita.IdPrestador,
            Puntuacion = puntuacion,
            Comentario = TrimOptional(comentario),
            FechaCreacion = DateTime.Now,
            Activo = true,
            ClienteNombreSnapshot = TrimRequired(cita.Cliente.Nombre),
            ServicioNombreSnapshot = TrimRequired(cita.Servicio.Nombre),
            PrestadorNombreSnapshot = TrimOptional(cita.Prestador?.Nombre)
        };

        ApplyEstadoInicial(resena, configuracion);
        ApplyAlertaOperativa(resena, configuracion);
        return resena;
    }

    private static void ApplyEstadoInicial(ResenaNegocio resena, ConfiguracionResenaNegocio configuracion)
    {
        if (configuracion.AutoaprobarResenas)
        {
            resena.Estado = ResenaNegocioEstados.Aprobada;
            resena.EsVisiblePublicamente = true;
            resena.FechaPublicacion = DateTime.Now;
            return;
        }

        resena.Estado = ResenaNegocioEstados.Pendiente;
        resena.EsVisiblePublicamente = false;
        resena.FechaPublicacion = null;
    }

    private static void ApplyAlertaOperativa(ResenaNegocio resena, ConfiguracionResenaNegocio configuracion)
    {
        if (resena.Puntuacion <= configuracion.PuntuacionMaximaAlertaOperativa)
        {
            resena.EsAlertaOperativa = true;
            resena.FechaAlertaOperativa ??= DateTime.Now;
            resena.MotivoAlertaOperativa = $"Puntuación {resena.Puntuacion}/5 menor o igual al umbral operativo {configuracion.PuntuacionMaximaAlertaOperativa}/5.";
            return;
        }

        resena.EsAlertaOperativa = false;
        resena.FechaAlertaOperativa = null;
        resena.MotivoAlertaOperativa = null;
    }

    private static ResenaResumenDto BuildResumen(int idNegocio, string negocio, IReadOnlyCollection<ResenaNegocio> resenas)
    {
        var total = resenas.Count;
        var publicadas = resenas
            .Where(item =>
                item.Estado == ResenaNegocioEstados.Aprobada &&
                item.EsVisiblePublicamente &&
                item.Activo)
            .ToArray();
        var promedio = publicadas.Length == 0 ? 0m : Math.Round((decimal)publicadas.Average(item => item.Puntuacion), 2);
        var distribucion = Enumerable.Range(1, 5)
            .Select(puntuacion => new ResenaPuntuacionDistribucionDto((byte)puntuacion, publicadas.Count(item => item.Puntuacion == puntuacion)))
            .ToArray();
        var servicios = publicadas
            .GroupBy(item => new { item.IdServicio, Nombre = DisplayServicio(item) })
            .Select(group => new ResenaServicioResumenDto(
                group.Key.IdServicio,
                group.Key.Nombre,
                Math.Round((decimal)group.Average(item => item.Puntuacion), 2),
                group.Count()))
            .OrderByDescending(item => item.PromedioPuntuacion)
            .ThenByDescending(item => item.TotalResenas)
            .Take(5)
            .ToArray();
        var prestadores = publicadas
            .Where(item => item.IdPrestador.HasValue)
            .GroupBy(item => new { IdPrestador = item.IdPrestador!.Value, Nombre = DisplayPrestador(item) ?? "Sin prestador" })
            .Select(group => new ResenaPrestadorResumenDto(
                group.Key.IdPrestador,
                group.Key.Nombre,
                Math.Round((decimal)group.Average(item => item.Puntuacion), 2),
                group.Count()))
            .OrderByDescending(item => item.PromedioPuntuacion)
            .ThenByDescending(item => item.TotalResenas)
            .Take(5)
            .ToArray();
        var ultimas = publicadas
            .OrderByDescending(item => item.FechaPublicacion ?? item.FechaCreacion)
            .Take(5)
            .Select(ToPublicDto)
            .ToArray();

        return new ResenaResumenDto(
            idNegocio,
            negocio,
            promedio,
            total,
            publicadas.Length,
            resenas.Count(item => item.Estado == ResenaNegocioEstados.Pendiente),
            resenas.Count(item => item.Estado == ResenaNegocioEstados.Rechazada),
            resenas.Count(item => item.Estado == ResenaNegocioEstados.Oculta),
            distribucion,
            servicios,
            prestadores,
            ultimas);
    }

    private static ReputacionNegocioDto BuildReputacion(
        int idNegocio,
        string negocio,
        DateTime fechaDesde,
        DateTime fechaHasta,
        bool incluyeNoPublicadas,
        IReadOnlyCollection<ResenaNegocio> resenas)
    {
        var total = resenas.Count;
        var totalPositivas = CountPositiveReviews(resenas);
        var servicios = resenas
            .GroupBy(item => new { item.IdServicio, Nombre = DisplayServicio(item) })
            .Select(group =>
            {
                var items = group.ToArray();
                var positivas = CountPositiveReviews(items);
                return new ReputacionServicioDto(
                    group.Key.IdServicio,
                    group.Key.Nombre,
                    AverageRating(items),
                    items.Length,
                    positivas,
                    Percentage(positivas, items.Length));
            })
            .OrderByDescending(item => item.PromedioPuntuacion)
            .ThenByDescending(item => item.TotalResenas)
            .ThenBy(item => item.Servicio)
            .ToArray();

        var prestadores = resenas
            .GroupBy(item => new { item.IdPrestador, Nombre = DisplayPrestador(item) ?? "Sin prestador" })
            .Select(group =>
            {
                var items = group.ToArray();
                var positivas = CountPositiveReviews(items);
                return new ReputacionPrestadorDto(
                    group.Key.IdPrestador,
                    group.Key.Nombre,
                    AverageRating(items),
                    items.Length,
                    positivas,
                    Percentage(positivas, items.Length));
            })
            .OrderByDescending(item => item.PromedioPuntuacion)
            .ThenByDescending(item => item.TotalResenas)
            .ThenBy(item => item.Prestador)
            .ToArray();

        return new ReputacionNegocioDto(
            idNegocio,
            negocio,
            fechaDesde,
            fechaHasta,
            incluyeNoPublicadas,
            AverageRating(resenas),
            total,
            totalPositivas,
            Percentage(totalPositivas, total),
            servicios,
            prestadores,
            BuildMonthlyReputation(resenas, fechaDesde, fechaHasta));
    }

    private static IReadOnlyCollection<ReputacionMensualDto> BuildMonthlyReputation(
        IReadOnlyCollection<ResenaNegocio> resenas,
        DateTime fechaDesde,
        DateTime fechaHasta)
    {
        var groups = resenas
            .GroupBy(item =>
            {
                var date = GetReputationDate(item);
                return (date.Year, date.Month);
            })
            .ToDictionary(group => group.Key, group => group.ToArray());

        var current = new DateTime(fechaDesde.Year, fechaDesde.Month, 1);
        var last = new DateTime(fechaHasta.Year, fechaHasta.Month, 1);
        var items = new List<ReputacionMensualDto>();

        while (current <= last)
        {
            groups.TryGetValue((current.Year, current.Month), out var monthReviews);
            monthReviews ??= [];
            var positivas = CountPositiveReviews(monthReviews);

            items.Add(new ReputacionMensualDto(
                current.Year,
                current.Month,
                current.ToString("yyyy-MM"),
                AverageRating(monthReviews),
                monthReviews.Length,
                positivas,
                Percentage(positivas, monthReviews.Length)));

            current = current.AddMonths(1);
        }

        return items;
    }

    private static ReputationPeriod ResolveReputationPeriod(ReputacionNegocioQuery query)
    {
        var today = DateTime.Now.Date;
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        var defaultFrom = currentMonth.AddMonths(-(query.MesesEvolucion - 1));
        var defaultTo = today;
        var fechaDesde = (query.FechaDesde ?? defaultFrom).Date;
        var fechaHasta = (query.FechaHasta ?? defaultTo).Date;

        return new ReputationPeriod(fechaDesde, fechaHasta, fechaHasta.AddDays(1));
    }

    private static DateTime GetReputationDate(ResenaNegocio item)
    {
        return (item.FechaPublicacion ?? item.FechaCreacion).Date;
    }

    private static decimal AverageRating(IReadOnlyCollection<ResenaNegocio> resenas)
    {
        return resenas.Count == 0 ? 0m : Math.Round((decimal)resenas.Average(item => item.Puntuacion), 2);
    }

    private static int CountPositiveReviews(IEnumerable<ResenaNegocio> resenas)
    {
        return resenas.Count(item => item.Puntuacion >= 4);
    }

    private static decimal Percentage(int part, int total)
    {
        return total == 0 ? 0m : Math.Round(part * 100m / total, 2);
    }

    private static ResenaNegocioDto ToDto(ResenaNegocio item)
    {
        return new ResenaNegocioDto(
            item.IdResenaNegocio,
            item.IdNegocio,
            item.Negocio?.Nombre ?? string.Empty,
            item.IdCita,
            item.Cita?.Codigo ?? string.Empty,
            item.IdCliente,
            DisplayCliente(item),
            ToPublicClientName(DisplayCliente(item)),
            item.UserId,
            item.IdServicio,
            DisplayServicio(item),
            item.IdPrestador,
            DisplayPrestador(item),
            item.Puntuacion,
            item.Comentario,
            item.Estado,
            item.EsVisiblePublicamente,
            item.FechaCreacion,
            item.FechaActualizacion,
            item.FechaPublicacion,
            item.ModeradoPorUserId,
            item.FechaModeracion,
            item.MotivoModeracion,
            item.RespuestaNegocio,
            item.RespondidoPorUserId,
            item.FechaRespuesta,
            item.EsAlertaOperativa,
            item.FechaAlertaOperativa,
            item.MotivoAlertaOperativa,
            item.Activo);
    }

    private static ResenaPublicaDto ToPublicDto(ResenaNegocio item)
    {
        return new ResenaPublicaDto(
            item.IdResenaNegocio,
            ToPublicClientName(DisplayCliente(item)),
            DisplayServicio(item),
            DisplayPrestador(item),
            item.Puntuacion,
            item.Comentario,
            item.RespuestaNegocio,
            item.FechaPublicacion ?? item.FechaCreacion,
            item.FechaRespuesta);
    }

    private static ConfiguracionResenaNegocioDto ToDto(ConfiguracionResenaNegocio item)
    {
        return new ConfiguracionResenaNegocioDto(
            item.IdConfiguracionResenaNegocio,
            item.IdNegocio,
            item.Negocio?.Nombre ?? string.Empty,
            item.ResenasActivas,
            item.AutoaprobarResenas,
            item.DiasMaximosParaCalificar,
            item.PuntuacionMaximaAlertaOperativa,
            item.PermitirRespuestaNegocio,
            item.MostrarResenasPublicas,
            item.FechaCreacion,
            item.FechaActualizacion);
    }

    private static SolicitudResenaPreviewDto InvalidPreview(string message, SolicitudResena? solicitud = null)
    {
        return new SolicitudResenaPreviewDto(
            false,
            solicitud?.Estado,
            solicitud?.Negocio?.Nombre,
            solicitud?.Cliente?.Nombre,
            solicitud?.Cita?.Servicio?.Nombre,
            solicitud?.Cita?.Prestador?.Nombre,
            solicitud?.Cita?.FechaFin,
            solicitud?.FechaExpiracion,
            message);
    }

    private async Task RegistrarAuditoriaAsync(
        CurrentUserContext currentUser,
        ResenaNegocio resena,
        string accion,
        string descripcion,
        object? previous,
        object? current,
        CancellationToken cancellationToken)
    {
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                resena.IdNegocio,
                "Resenas",
                accion,
                nameof(ResenaNegocio),
                resena.IdResenaNegocio.ToString(),
                descripcion,
                previous,
                current),
            cancellationToken);
    }

    private static object ToAuditSnapshot(ResenaNegocio item)
    {
        return new
        {
            item.IdResenaNegocio,
            item.IdNegocio,
            item.IdCita,
            item.IdCliente,
            item.IdServicio,
            item.IdPrestador,
            item.Puntuacion,
            item.Comentario,
            item.Estado,
            item.EsVisiblePublicamente,
            item.FechaPublicacion,
            item.MotivoModeracion,
            item.RespuestaNegocio,
            item.EsAlertaOperativa,
            item.FechaAlertaOperativa,
            item.MotivoAlertaOperativa,
            item.Activo
        };
    }

    private static object ToAuditSnapshot(ConfiguracionResenaNegocio item)
    {
        return new
        {
            item.IdConfiguracionResenaNegocio,
            item.IdNegocio,
            item.ResenasActivas,
            item.AutoaprobarResenas,
            item.DiasMaximosParaCalificar,
            item.PuntuacionMaximaAlertaOperativa,
            item.PermitirRespuestaNegocio,
            item.MostrarResenasPublicas
        };
    }

    private string BuildResenaLink(string token)
    {
        var template = configuration["Reviews:CreateUrl"];
        if (string.IsNullOrWhiteSpace(template))
        {
            template = "http://localhost:4200/resenas/crear?token={token}";
        }

        if (template.Contains("{token}", StringComparison.OrdinalIgnoreCase))
        {
            return template.Replace("{token}", Uri.EscapeDataString(token), StringComparison.OrdinalIgnoreCase);
        }

        var separator = template.Contains('?') ? "&" : "?";
        return $"{template}{separator}token={Uri.EscapeDataString(token)}";
    }

    private static string DisplayCliente(ResenaNegocio item)
    {
        return string.IsNullOrWhiteSpace(item.ClienteNombreSnapshot)
            ? item.Cliente?.Nombre ?? "Cliente"
            : item.ClienteNombreSnapshot;
    }

    private static string DisplayServicio(ResenaNegocio item)
    {
        return string.IsNullOrWhiteSpace(item.ServicioNombreSnapshot)
            ? item.Servicio?.Nombre ?? "Servicio"
            : item.ServicioNombreSnapshot;
    }

    private static string? DisplayPrestador(ResenaNegocio item)
    {
        return string.IsNullOrWhiteSpace(item.PrestadorNombreSnapshot)
            ? item.Prestador?.Nombre
            : item.PrestadorNombreSnapshot;
    }

    private static string ToPublicClientName(string name)
    {
        var trimmed = TrimRequired(name);
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "Cliente";
        }

        if (parts.Length == 1)
        {
            return parts[0];
        }

        return $"{parts[0]} {parts[^1][0]}.";
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static string TrimRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? TrimOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record ResenaAccess(
        bool Exists,
        bool CanViewAll,
        IReadOnlyCollection<int> PrestadorIds)
    {
        public bool CanView => CanViewAll || PrestadorIds.Count > 0;
    }

    private readonly record struct NotificationMetadata(
        int IdTipoNotificacion,
        int IdCanalNotificacion,
        int IdEstadoNotificacion);

    private readonly record struct ReputationPeriod(
        DateTime FechaDesde,
        DateTime FechaHasta,
        DateTime FechaHastaExclusiva);
}
