using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Auditoria;

public sealed class AuditoriaService(ReservaFlowDbContext dbContext) : IAuditoriaService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task RegistrarAsync(
        CurrentUserContext currentUser,
        AuditoriaRegistro registro,
        CancellationToken cancellationToken)
    {
        var cambios = BuildChanges(registro.ValoresAnteriores, registro.ValoresNuevos);
        var userId = await ResolveAuditUserIdAsync(currentUser, cancellationToken);

        dbContext.AuditoriaEventos.Add(new AuditoriaEvento
        {
            IdNegocio = registro.IdNegocio,
            UserId = userId,
            Categoria = TrimRequired(registro.Categoria, 80),
            Accion = TrimRequired(registro.Accion, 80),
            Entidad = TrimRequired(registro.Entidad, 120),
            EntidadId = TrimOptional(registro.EntidadId, 128),
            Descripcion = TrimRequired(registro.Descripcion, 500),
            ValoresAnterioresJson = SerializeOptional(registro.ValoresAnteriores),
            ValoresNuevosJson = SerializeOptional(registro.ValoresNuevos),
            CambiosJson = cambios.Count == 0 ? null : JsonSerializer.Serialize(cambios, JsonOptions),
            MetadataJson = SerializeOptional(registro.Metadata),
            FechaCreacion = DateTime.Now
        });
    }

    public async Task<ServiceResult<PagedResult<AuditoriaEventoDto>>> GetByNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        AuditoriaQuery query,
        CancellationToken cancellationToken)
    {
        var negocioExists = await dbContext.Negocios
            .AsNoTracking()
            .AnyAsync(negocio => negocio.IdNegocio == idNegocio, cancellationToken);

        if (!negocioExists)
        {
            return ServiceResult<PagedResult<AuditoriaEventoDto>>.NotFound("El negocio no existe.");
        }

        if (!await CanViewAuditoriaAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<PagedResult<AuditoriaEventoDto>>.Forbidden("No tienes acceso para ver la auditoría de este negocio.");
        }

        var eventosQuery = dbContext.AuditoriaEventos
            .AsNoTracking()
            .Include(evento => evento.Negocio)
            .Include(evento => evento.Usuario)
            .Where(evento => evento.IdNegocio == idNegocio);

        return await ToPagedResultAsync(eventosQuery, query, cancellationToken);
    }

    public async Task<ServiceResult<PagedResult<AuditoriaEventoDto>>> GetGlobalAsync(
        CurrentUserContext currentUser,
        AuditoriaQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsSuperAdmin)
        {
            return ServiceResult<PagedResult<AuditoriaEventoDto>>.Forbidden("Solo un SuperAdmin puede ver la auditoría global.");
        }

        var eventosQuery = dbContext.AuditoriaEventos
            .AsNoTracking()
            .Include(evento => evento.Negocio)
            .Include(evento => evento.Usuario)
            .AsQueryable();

        return await ToPagedResultAsync(eventosQuery, query, cancellationToken);
    }

    public async Task<ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetCategoriasSelectAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateSelectAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var eventosQuery = ApplySelectFilters(BuildSelectQuery(idNegocio), query);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.Categoria.Contains(search));
        }

        var items = await eventosQuery
            .GroupBy(evento => evento.Categoria)
            .Select(group => new { Value = group.Key, Cantidad = group.Count() })
            .OrderBy(item => item.Value)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>.Success(
            items.Select(item => new AuditoriaFiltroSelectDto(item.Value, item.Value, item.Cantidad)).ToArray());
    }

    public async Task<ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetAccionesSelectAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateSelectAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var eventosQuery = ApplySelectFilters(BuildSelectQuery(idNegocio), query);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.Accion.Contains(search));
        }

        var items = await eventosQuery
            .GroupBy(evento => evento.Accion)
            .Select(group => new { Value = group.Key, Cantidad = group.Count() })
            .OrderBy(item => item.Value)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>.Success(
            items.Select(item => new AuditoriaFiltroSelectDto(item.Value, item.Value, item.Cantidad)).ToArray());
    }

    public async Task<ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetEntidadesSelectAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateSelectAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var eventosQuery = ApplySelectFilters(BuildSelectQuery(idNegocio), query);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.Entidad.Contains(search));
        }

        var items = await eventosQuery
            .GroupBy(evento => evento.Entidad)
            .Select(group => new { Value = group.Key, Cantidad = group.Count() })
            .OrderBy(item => item.Value)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>.Success(
            items.Select(item => new AuditoriaFiltroSelectDto(item.Value, item.Value, item.Cantidad)).ToArray());
    }

    public async Task<ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetUsuariosSelectAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateSelectAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var eventosQuery = ApplySelectFilters(BuildSelectQuery(idNegocio), query)
            .Where(evento => evento.UserId != null);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            eventosQuery = eventosQuery.Where(evento =>
                evento.UserId!.Contains(search) ||
                (evento.Usuario != null && evento.Usuario.Email != null && evento.Usuario.Email.Contains(search)));
        }

        var items = await eventosQuery
            .GroupBy(evento => new
            {
                UserId = evento.UserId!,
                Email = evento.Usuario != null ? evento.Usuario.Email : null
            })
            .Select(group => new
            {
                group.Key.UserId,
                group.Key.Email,
                Cantidad = group.Count()
            })
            .OrderBy(item => item.Email ?? item.UserId)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>.Success(
            items
                .Select(item => new AuditoriaFiltroSelectDto(
                    item.UserId,
                    string.IsNullOrWhiteSpace(item.Email) ? item.UserId : item.Email,
                    item.Cantidad))
                .ToArray());
    }

    private static IQueryable<AuditoriaEvento> ApplyFilters(
        IQueryable<AuditoriaEvento> eventosQuery,
        AuditoriaQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            eventosQuery = eventosQuery.Where(evento =>
                evento.Descripcion.Contains(search) ||
                evento.Categoria.Contains(search) ||
                evento.Accion.Contains(search) ||
                evento.Entidad.Contains(search) ||
                (evento.EntidadId != null && evento.EntidadId.Contains(search)) ||
                (evento.Usuario != null && evento.Usuario.Email != null && evento.Usuario.Email.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Categoria))
        {
            var categoria = query.Categoria.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.Categoria == categoria);
        }

        if (!string.IsNullOrWhiteSpace(query.Accion))
        {
            var accion = query.Accion.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.Accion == accion);
        }

        if (!string.IsNullOrWhiteSpace(query.Entidad))
        {
            var entidad = query.Entidad.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.Entidad == entidad);
        }

        if (!string.IsNullOrWhiteSpace(query.EntidadId))
        {
            var entidadId = query.EntidadId.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.EntidadId == entidadId);
        }

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            var userId = query.UserId.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.UserId == userId);
        }

        if (query.IdNegocio.HasValue)
        {
            eventosQuery = eventosQuery.Where(evento => evento.IdNegocio == query.IdNegocio.Value);
        }

        if (query.FechaDesde.HasValue)
        {
            eventosQuery = eventosQuery.Where(evento => evento.FechaCreacion >= query.FechaDesde.Value);
        }

        if (query.FechaHasta.HasValue)
        {
            eventosQuery = eventosQuery.Where(evento => evento.FechaCreacion <= query.FechaHasta.Value);
        }

        return eventosQuery;
    }

    private IQueryable<AuditoriaEvento> BuildSelectQuery(int? idNegocio)
    {
        var eventosQuery = dbContext.AuditoriaEventos
            .AsNoTracking()
            .Include(evento => evento.Usuario)
            .AsQueryable();

        if (idNegocio.HasValue)
        {
            eventosQuery = eventosQuery.Where(evento => evento.IdNegocio == idNegocio.Value);
        }

        return eventosQuery;
    }

    private static IQueryable<AuditoriaEvento> ApplySelectFilters(
        IQueryable<AuditoriaEvento> eventosQuery,
        AuditoriaFiltroSelectQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Categoria))
        {
            var categoria = query.Categoria.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.Categoria == categoria);
        }

        if (!string.IsNullOrWhiteSpace(query.Accion))
        {
            var accion = query.Accion.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.Accion == accion);
        }

        if (!string.IsNullOrWhiteSpace(query.Entidad))
        {
            var entidad = query.Entidad.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.Entidad == entidad);
        }

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            var userId = query.UserId.Trim();
            eventosQuery = eventosQuery.Where(evento => evento.UserId == userId);
        }

        if (query.FechaDesde.HasValue)
        {
            eventosQuery = eventosQuery.Where(evento => evento.FechaCreacion >= query.FechaDesde.Value);
        }

        if (query.FechaHasta.HasValue)
        {
            eventosQuery = eventosQuery.Where(evento => evento.FechaCreacion <= query.FechaHasta.Value);
        }

        return eventosQuery;
    }

    private static async Task<ServiceResult<PagedResult<AuditoriaEventoDto>>> ToPagedResultAsync(
        IQueryable<AuditoriaEvento> eventosQuery,
        AuditoriaQuery query,
        CancellationToken cancellationToken)
    {
        eventosQuery = ApplyFilters(eventosQuery, query);

        var totalItems = await eventosQuery.CountAsync(cancellationToken);
        var eventos = await eventosQuery
            .OrderByDescending(evento => evento.FechaCreacion)
            .ThenByDescending(evento => evento.IdAuditoriaEvento)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        var items = eventos.Select(ToDto).ToArray();

        return ServiceResult<PagedResult<AuditoriaEventoDto>>.Success(
            new PagedResult<AuditoriaEventoDto>(items, query.PageNumber, query.PageSize, totalItems));
    }

    private async Task<bool> CanViewAuditoriaAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        return currentUser.IsAuthenticated &&
            await dbContext.NegocioUsuarios.AnyAsync(
                item =>
                    item.IdNegocio == idNegocio &&
                    item.UserId == currentUser.UserId &&
                    item.Activo &&
                    (item.RolNegocio.Nombre == OwnerRoleName || item.RolNegocio.Nombre == AdminRoleName),
                cancellationToken);
    }

    private async Task<ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>?> ValidateSelectAccessAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        CancellationToken cancellationToken)
    {
        if (!idNegocio.HasValue)
        {
            return currentUser.IsSuperAdmin
                ? null
                : ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>.Forbidden("Solo un SuperAdmin puede ver filtros globales de auditoría.");
        }

        var negocioExists = await dbContext.Negocios
            .AsNoTracking()
            .AnyAsync(negocio => negocio.IdNegocio == idNegocio.Value, cancellationToken);

        if (!negocioExists)
        {
            return ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>.NotFound("El negocio no existe.");
        }

        return await CanViewAuditoriaAsync(currentUser, idNegocio.Value, cancellationToken)
            ? null
            : ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>.Forbidden("No tienes acceso para ver filtros de auditoría de este negocio.");
    }

    private static AuditoriaEventoDto ToDto(AuditoriaEvento evento)
    {
        return new AuditoriaEventoDto(
            evento.IdAuditoriaEvento,
            evento.IdNegocio,
            evento.Negocio?.Nombre,
            evento.UserId,
            evento.Usuario?.Email,
            evento.Categoria,
            evento.Accion,
            evento.Entidad,
            evento.EntidadId,
            evento.Descripcion,
            DeserializeChanges(evento.CambiosJson),
            evento.ValoresAnterioresJson,
            evento.ValoresNuevosJson,
            evento.MetadataJson,
            evento.FechaCreacion);
    }

    private static IReadOnlyCollection<AuditoriaCambioDto> BuildChanges(object? previous, object? current)
    {
        var previousNode = ToJsonNode(previous);
        var currentNode = ToJsonNode(current);
        var previousObject = previousNode as JsonObject;
        var currentObject = currentNode as JsonObject;

        if (previousObject is null && currentObject is null)
        {
            return JsonNode.DeepEquals(previousNode, currentNode)
                ? []
                : [new AuditoriaCambioDto("valor", FormatValue(previousNode), FormatValue(currentNode))];
        }

        previousObject ??= new JsonObject();
        currentObject ??= new JsonObject();

        var keys = previousObject
            .Select(item => item.Key)
            .Union(currentObject.Select(item => item.Key), StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key)
            .ToArray();

        var changes = new List<AuditoriaCambioDto>();
        foreach (var key in keys)
        {
            previousObject.TryGetPropertyValue(key, out var previousValue);
            currentObject.TryGetPropertyValue(key, out var currentValue);

            if (JsonNode.DeepEquals(previousValue, currentValue))
            {
                continue;
            }

            changes.Add(new AuditoriaCambioDto(key, FormatValue(previousValue), FormatValue(currentValue)));
        }

        return changes;
    }

    private static JsonNode? ToJsonNode(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(value, JsonOptions);
    }

    private static string? FormatValue(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue value)
        {
            if (value.TryGetValue<string>(out var stringValue))
            {
                return stringValue;
            }

            if (value.TryGetValue<bool>(out var boolValue))
            {
                return boolValue ? "true" : "false";
            }

            if (value.TryGetValue<decimal>(out var decimalValue))
            {
                return decimalValue.ToString("0.##");
            }

            if (value.TryGetValue<DateTime>(out var dateTimeValue))
            {
                return dateTimeValue.ToString("O");
            }
        }

        return node.ToJsonString(JsonOptions);
    }

    private static IReadOnlyCollection<AuditoriaCambioDto> DeserializeChanges(string? cambiosJson)
    {
        if (string.IsNullOrWhiteSpace(cambiosJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyCollection<AuditoriaCambioDto>>(cambiosJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? SerializeOptional(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
    }

    private static string TrimRequired(string value, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "Sin detalle" : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private async Task<string?> ResolveAuditUserIdAsync(
        CurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return null;
        }

        var userId = currentUser.UserId.Trim();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken)
            ? userId
            : null;
    }
}
