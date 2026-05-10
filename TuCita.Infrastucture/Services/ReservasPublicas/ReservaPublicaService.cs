using Microsoft.EntityFrameworkCore;
using TuCita.Application.Citas;
using TuCita.Application.Clientes;
using TuCita.Application.Common;
using TuCita.Application.Disponibilidad;
using TuCita.Application.Notificaciones;
using TuCita.Application.ReservasPublicas;
using TuCita.Infrastucture.Citas;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Pagos;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.ReservasPublicas;

public sealed class ReservaPublicaService(
    ReservaFlowDbContext dbContext,
    IDisponibilidadService disponibilidadService,
    INotificacionService notificacionService,
    IClienteResolverService clienteResolverService,
    ICitaPagoImpactService citaPagoImpactService) : IReservaPublicaService
{
    private const string PendingStateName = "Pendiente";
    private const string ConfirmedStateName = "Confirmada";
    private const string PaymentPendingStateName = "Pendiente de pago";
    private const string CancelledStateName = "Cancelada";
    private const string RescheduledStateName = "Reagendada";
    private const string ApprovedReviewStateName = "Aprobada";
    private const int DefaultMinHorasAnticipacion = 2;
    private const int DefaultMaxDiasAdelanto = 30;
    private const int DefaultHorasLimiteCancelacion = 6;
    private const int DefaultMaxCitasActivasPorCliente = 1;

    public async Task<PagedResult<PublicNegocioSearchDto>> SearchNegociosAsync(
        PublicNegocioQuery query,
        CancellationToken cancellationToken)
    {
        var negociosQuery = dbContext.Negocios
            .AsNoTracking()
            .Where(negocio => negocio.Activo);

        if (query.IdRubro.HasValue)
        {
            negociosQuery = negociosQuery.Where(negocio => negocio.IdRubro == query.IdRubro.Value);
        }

        if (query.SoloConServiciosActivos)
        {
            negociosQuery = negociosQuery.Where(negocio => negocio.Servicios.Any(servicio => servicio.Activo));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchPattern = $"%{query.Search.Trim()}%";
            negociosQuery = negociosQuery.Where(negocio =>
                EF.Functions.Like(negocio.Nombre, searchPattern) ||
                EF.Functions.Like(negocio.Slug, searchPattern) ||
                EF.Functions.Like(negocio.Rubro.Nombre, searchPattern) ||
                (negocio.Descripcion != null && EF.Functions.Like(negocio.Descripcion, searchPattern)) ||
                (negocio.Direccion != null && EF.Functions.Like(negocio.Direccion, searchPattern)));
        }

        var totalItems = await negociosQuery.CountAsync(cancellationToken);
        var negocioItems = await negociosQuery
            .OrderBy(negocio => negocio.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(negocio => new
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
                ServiciosActivos = negocio.Servicios.Count(servicio => servicio.Activo)
            })
            .ToArrayAsync(cancellationToken);

        var reviewSummaries = await GetPublicReviewSummariesAsync(
            negocioItems.Select(negocio => negocio.IdNegocio).ToArray(),
            cancellationToken);

        var items = negocioItems
            .Select(negocio =>
            {
                reviewSummaries.TryGetValue(negocio.IdNegocio, out var reviewSummary);

                return new PublicNegocioSearchDto(
                    negocio.IdNegocio,
                    negocio.IdRubro,
                    negocio.Rubro,
                    negocio.Nombre,
                    negocio.Slug,
                    negocio.Descripcion,
                    negocio.LogoUrl,
                    negocio.Direccion,
                    negocio.Telefono,
                    negocio.Email,
                    negocio.ServiciosActivos,
                    reviewSummary?.PromedioPuntuacion ?? 0m,
                    reviewSummary?.TotalResenas ?? 0,
                    reviewSummary?.TotalPublicadas ?? 0,
                    reviewSummary?.DistribucionEstrellas ?? EmptyStarDistribution());
            })
            .ToArray();

        return new PagedResult<PublicNegocioSearchDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<PublicNegocioDto>> GetNegocioAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var negocio = await GetNegocioBySlugQuery(slug)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return negocio is null
            ? ServiceResult<PublicNegocioDto>.NotFound("El negocio no existe o no está activo.")
            : ServiceResult<PublicNegocioDto>.Success(ToPublicNegocioDto(negocio));
    }

    public async Task<ServiceResult<IReadOnlyCollection<PublicServicioDto>>> GetServiciosAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var negocio = await GetNegocioBySlugQuery(slug)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<IReadOnlyCollection<PublicServicioDto>>.NotFound("El negocio no existe o no está activo.");
        }

        var servicios = await dbContext.Servicios
            .AsNoTracking()
            .Include(servicio => servicio.CategoriaServicio)
            .Where(servicio => servicio.IdNegocio == negocio.IdNegocio && servicio.Activo)
            .OrderBy(servicio => servicio.CategoriaServicio == null ? string.Empty : servicio.CategoriaServicio.Nombre)
            .ThenBy(servicio => servicio.Nombre)
            .Select(servicio => new PublicServicioDto(
                servicio.IdServicio,
                servicio.IdCategoriaServicio,
                servicio.CategoriaServicio == null ? null : servicio.CategoriaServicio.Nombre,
                servicio.Nombre,
                servicio.Descripcion,
                servicio.DuracionMinutos,
                servicio.Precio,
                servicio.RequiereProfesional,
                servicio.RequierePagoAnticipado,
                servicio.TiempoPreparacionMinutos))
            .ToArrayAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<PublicServicioDto>>.Success(servicios);
    }

    public async Task<ServiceResult<IReadOnlyCollection<PublicPrestadorDto>>> GetPrestadoresAsync(
        string slug,
        int idServicio,
        CancellationToken cancellationToken)
    {
        var negocio = await GetNegocioBySlugQuery(slug)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<IReadOnlyCollection<PublicPrestadorDto>>.NotFound("El negocio no existe o no está activo.");
        }

        var servicioExists = await dbContext.Servicios.AnyAsync(
            servicio =>
                servicio.IdNegocio == negocio.IdNegocio &&
                servicio.IdServicio == idServicio &&
                servicio.Activo,
            cancellationToken);

        if (!servicioExists)
        {
            return ServiceResult<IReadOnlyCollection<PublicPrestadorDto>>.Validation([
                new ValidationError(nameof(idServicio), "El servicio indicado no existe o no está activo para este negocio.")
            ]);
        }

        var prestadores = await dbContext.PrestadorServicios
            .AsNoTracking()
            .Where(relacion =>
                relacion.IdNegocio == negocio.IdNegocio &&
                relacion.IdServicio == idServicio &&
                relacion.Activo &&
                relacion.Prestador.Activo)
            .OrderBy(relacion => relacion.Prestador.Nombre)
            .Select(relacion => new PublicPrestadorDto(
                relacion.IdPrestador,
                relacion.Prestador.TipoPrestador.Nombre,
                relacion.Prestador.Nombre,
                relacion.Prestador.Capacidad))
            .ToArrayAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<PublicPrestadorDto>>.Success(prestadores);
    }

    public async Task<ServiceResult<DisponibilidadDto>> GetDisponibilidadAsync(
        string slug,
        DisponibilidadQuery query,
        CancellationToken cancellationToken)
    {
        var negocio = await GetNegocioBySlugQuery(slug)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<DisponibilidadDto>.NotFound("El negocio no existe o no está activo.");
        }

        return await disponibilidadService.GetDisponibilidadAsync(negocio.IdNegocio, query, cancellationToken);
    }

    public async Task<ServiceResult<IReadOnlyCollection<PublicCampoReservaDto>>> GetCamposReservaAsync(
        string slug,
        int? idServicio,
        CancellationToken cancellationToken)
    {
        var negocio = await GetNegocioBySlugQuery(slug)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<IReadOnlyCollection<PublicCampoReservaDto>>.NotFound("El negocio no existe o no está activo.");
        }

        if (idServicio.HasValue)
        {
            var servicioExists = await dbContext.Servicios.AnyAsync(
                servicio =>
                    servicio.IdNegocio == negocio.IdNegocio &&
                    servicio.IdServicio == idServicio.Value &&
                    servicio.Activo,
                cancellationToken);

            if (!servicioExists)
            {
                return ServiceResult<IReadOnlyCollection<PublicCampoReservaDto>>.Validation([
                    new ValidationError(nameof(idServicio), "El servicio indicado no existe o no está activo para este negocio.")
                ]);
            }
        }

        var campos = await dbContext.CamposReserva
            .AsNoTracking()
            .Include(campo => campo.Servicio)
            .Include(campo => campo.TipoCampo)
            .Include(campo => campo.Opciones)
            .Where(campo =>
                campo.IdNegocio == negocio.IdNegocio &&
                campo.Activo &&
                (!campo.IdServicio.HasValue || campo.IdServicio == idServicio))
            .OrderBy(campo => campo.Orden)
            .ThenBy(campo => campo.Etiqueta)
            .ToArrayAsync(cancellationToken);

        var result = campos.Select(ToPublicCampoReservaDto).ToArray();
        return ServiceResult<IReadOnlyCollection<PublicCampoReservaDto>>.Success(result);
    }

    public async Task<ServiceResult<PublicReglaReservaDto>> GetReglasReservaAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var negocio = await GetNegocioBySlugQuery(slug)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<PublicReglaReservaDto>.NotFound("El negocio no existe o no está activo.");
        }

        var regla = await dbContext.ReglasReserva
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == negocio.IdNegocio, cancellationToken);

        return ServiceResult<PublicReglaReservaDto>.Success(ToPublicReglaReservaDto(regla));
    }

    public async Task<ServiceResult<PublicReservaDto>> GetReservaByCodigoAsync(
        string slug,
        string codigo,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim();
        var normalizedCodigo = codigo.Trim();

        var cita = await dbContext.Citas
            .AsNoTracking()
            .Include(item => item.Negocio)
            .Include(item => item.Cliente)
            .Include(item => item.Servicio)
            .Include(item => item.Prestador)
            .Include(item => item.EstadoCita)
            .FirstOrDefaultAsync(
                item =>
                    item.Negocio.Slug == normalizedSlug &&
                    item.Negocio.Activo &&
                    item.Codigo == normalizedCodigo,
                cancellationToken);

        return cita is null
            ? ServiceResult<PublicReservaDto>.NotFound("La reserva no existe para este negocio.")
            : ServiceResult<PublicReservaDto>.Success(ToPublicReservaDto(cita));
    }

    public async Task<ServiceResult<PublicReservaMisDatosDto>> GetMisDatosReservaAsync(
        CurrentUserContext currentUser,
        string slug,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<PublicReservaMisDatosDto>.Forbidden("Debes iniciar sesión para obtener tus datos de reserva.");
        }

        var negocioExists = await GetNegocioBySlugQuery(slug)
            .AsNoTracking()
            .AnyAsync(cancellationToken);

        if (!negocioExists)
        {
            return ServiceResult<PublicReservaMisDatosDto>.NotFound("El negocio no existe o no está activo.");
        }

        var data = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == currentUser.UserId)
            .Select(user => new
            {
                user.Email,
                user.UserName,
                Perfil = dbContext.UsuariosPerfil
                    .Where(profile => profile.UserId == user.Id)
                    .Select(profile => new
                    {
                        profile.Nombre,
                        profile.Apellido,
                        profile.NombreCompleto,
                        profile.Rut,
                        Telefono = profile.Contacto == null ? null : profile.Contacto.TelefonoAlternativo
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
        {
            return ServiceResult<PublicReservaMisDatosDto>.Forbidden("El usuario autenticado no existe.");
        }

        var nombreCliente = BuildNombreCliente(
            data.Perfil?.NombreCompleto,
            data.Perfil?.Nombre,
            data.Perfil?.Apellido,
            data.UserName,
            data.Email);

        return ServiceResult<PublicReservaMisDatosDto>.Success(new PublicReservaMisDatosDto(
            nombreCliente,
            data.Email ?? string.Empty,
            data.Perfil?.Telefono,
            data.Perfil?.Rut,
            EmailBloqueado: true));
    }

    public async Task<ServiceResult<PublicReservaDto>> CreateReservaAsync(
        CurrentUserContext currentUser,
        string slug,
        CreateReservaPublicaRequest request,
        CancellationToken cancellationToken)
    {
        var negocio = await GetNegocioBySlugQuery(slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<PublicReservaDto>.NotFound("El negocio no existe o no está activo.");
        }

        return await CitaConcurrencyGuard.ExecuteWithBusinessScheduleLockAsync(
            dbContext,
            negocio.IdNegocio,
            async () =>
            {
                var validationErrors = await ValidateReservaAsync(currentUser, negocio.IdNegocio, request, cancellationToken);
                if (validationErrors.Count > 0)
                {
                    return ServiceResult<PublicReservaDto>.Validation(validationErrors);
                }

                var servicio = await dbContext.Servicios
                    .FirstAsync(
                        item => item.IdNegocio == negocio.IdNegocio && item.IdServicio == request.IdServicio && item.Activo,
                        cancellationToken);
                var regla = await dbContext.ReglasReserva
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.IdNegocio == negocio.IdNegocio, cancellationToken);
                var estado = await GetDefaultEstadoAsync(
                    servicio.RequierePagoAnticipado,
                    regla?.RequiereConfirmacionManual ?? false,
                    cancellationToken);

                if (estado is null)
                {
                    return ServiceResult<PublicReservaDto>.Validation([
                        new ValidationError(string.Empty, "No existe un estado de cita activo para crear la reserva.")
                    ]);
                }

                var clienteResult = await clienteResolverService.ResolveForReservaPublicaAsync(
                    currentUser,
                    new ResolveClienteReservaRequest(
                        negocio.IdNegocio,
                        request.NombreCliente,
                        request.Email,
                        request.Telefono,
                        request.Rut),
                    cancellationToken);

                if (!clienteResult.Succeeded || clienteResult.Data is null)
                {
                    return ToPublicReservaFailure(clienteResult);
                }

                var cliente = clienteResult.Data;
                var activeLimitErrors = await ValidateMaxCitasActivasAsync(cliente.IdNegocio, cliente.IdCliente, cancellationToken);
                if (activeLimitErrors.Count > 0)
                {
                    return ServiceResult<PublicReservaDto>.Validation(activeLimitErrors);
                }

                var cita = new Cita
                {
                    IdNegocio = negocio.IdNegocio,
                    IdCliente = cliente.IdCliente,
                    IdServicio = servicio.IdServicio,
                    IdPrestador = request.IdPrestador,
                    IdEstadoCita = estado.IdEstadoCita,
                    Codigo = await GenerateCodigoAsync(negocio, cancellationToken),
                    FechaInicio = request.FechaInicio,
                    FechaFin = request.FechaInicio.AddMinutes(servicio.DuracionMinutos),
                    ComentarioCliente = request.ComentarioCliente?.Trim(),
                    PrecioEstimado = servicio.Precio
                };

                dbContext.Citas.Add(cita);
                AddCampoValores(cita, request.CamposValor);
                cita.Historial.Add(new CitaHistorial
                {
                    IdNegocio = negocio.IdNegocio,
                    IdEstadoAnterior = null,
                    IdEstadoNuevo = estado.IdEstadoCita,
                    UserId = currentUser.IsAuthenticated ? currentUser.UserId : null,
                    Observacion = "Reserva creada desde página pública."
                });

                await dbContext.SaveChangesAsync(cancellationToken);
                await notificacionService.CrearPorCitaCreadaAsync(cita.IdCita, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                var created = await dbContext.Citas
                    .AsNoTracking()
                    .Include(item => item.Negocio)
                    .Include(item => item.Cliente)
                    .Include(item => item.Servicio)
                    .Include(item => item.Prestador)
                    .Include(item => item.EstadoCita)
                    .FirstAsync(item => item.IdCita == cita.IdCita, cancellationToken);

                return ServiceResult<PublicReservaDto>.Success(ToPublicReservaDto(created));
            },
            cancellationToken);
    }

    public async Task<ServiceResult<PublicReservaDto>> CancelReservaAsync(
        string slug,
        string codigo,
        CancelReservaPublicaRequest request,
        CancellationToken cancellationToken)
    {
        var cita = await GetEditablePublicReservaAsync(slug, codigo, cancellationToken);
        if (cita is null)
        {
            return ServiceResult<PublicReservaDto>.NotFound("La reserva no existe para este negocio.");
        }

        var validationErrors = await ValidatePublicClientActionAsync(cita, request.Email, request.Telefono, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<PublicReservaDto>.Validation(validationErrors);
        }

        var estado = await dbContext.EstadosCita
            .FirstOrDefaultAsync(item => item.Nombre == CancelledStateName && item.Activo, cancellationToken);

        if (estado is null)
        {
            return ServiceResult<PublicReservaDto>.Validation([
                new ValidationError(string.Empty, $"El estado de cita '{CancelledStateName}' no existe o no está activo.")
            ]);
        }

        var pagoSnapshot = citaPagoImpactService.Capture(cita);
        var estadoAnterior = cita.IdEstadoCita;
        cita.IdEstadoCita = estado.IdEstadoCita;
        cita.FechaActualizacion = DateTime.Now;
        AddHistorial(cita, estadoAnterior, estado.IdEstadoCita, request.Observacion ?? "Reserva cancelada por el cliente desde página pública.");
        await citaPagoImpactService.RegistrarCancelacionAsync(
            cita,
            pagoSnapshot,
            null,
            request.Observacion ?? "Reserva cancelada por el cliente desde página pública.",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await notificacionService.CrearPorCambioEstadoAsync(cita.IdCita, estado.Nombre, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await GetReservaDetalleQuery()
            .FirstAsync(item => item.IdCita == cita.IdCita, cancellationToken);

        return ServiceResult<PublicReservaDto>.Success(ToPublicReservaDto(updated));
    }

    public async Task<ServiceResult<PublicReservaDto>> ReagendarReservaAsync(
        string slug,
        string codigo,
        ReagendarReservaPublicaRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim();
        var normalizedCodigo = codigo.Trim();
        var idNegocio = await dbContext.Citas
            .AsNoTracking()
            .Where(item =>
                item.Negocio.Slug == normalizedSlug &&
                item.Negocio.Activo &&
                item.Codigo == normalizedCodigo)
            .Select(item => (int?)item.IdNegocio)
            .FirstOrDefaultAsync(cancellationToken);

        if (!idNegocio.HasValue)
        {
            return ServiceResult<PublicReservaDto>.NotFound("La reserva no existe para este negocio.");
        }

        return await CitaConcurrencyGuard.ExecuteWithBusinessScheduleLockAsync(
            dbContext,
            idNegocio.Value,
            async () =>
            {
                var cita = await GetEditablePublicReservaAsync(slug, codigo, cancellationToken);
                if (cita is null)
                {
                    return ServiceResult<PublicReservaDto>.NotFound("La reserva no existe para este negocio.");
                }

                var clientErrors = await ValidatePublicClientActionAsync(cita, request.Email, request.Telefono, cancellationToken);
                if (clientErrors.Count > 0)
                {
                    return ServiceResult<PublicReservaDto>.Validation(clientErrors);
                }

                var idPrestador = request.IdPrestador ?? cita.IdPrestador;
                var disponibilidad = await disponibilidadService.GetDisponibilidadAsync(
                    cita.IdNegocio,
                    new DisponibilidadQuery
                    {
                        IdServicio = cita.IdServicio,
                        IdPrestador = idPrestador,
                        Fecha = DateOnly.FromDateTime(request.FechaInicio)
                    },
                    cancellationToken);

                if (!disponibilidad.Succeeded || disponibilidad.Data is null)
                {
                    return ServiceResult<PublicReservaDto>.Validation(disponibilidad.ValidationErrors.Count > 0
                        ? disponibilidad.ValidationErrors
                        : disponibilidad.Errors.Select(error => new ValidationError(string.Empty, error)));
                }

                var hasSlot = disponibilidad.Data.Slots.Any(slot =>
                    slot.FechaInicio == request.FechaInicio &&
                    slot.IdPrestador == idPrestador);

                if (!hasSlot)
                {
                    return ServiceResult<PublicReservaDto>.Validation([
                        new ValidationError(nameof(ReagendarReservaPublicaRequest.FechaInicio), "El horario seleccionado ya no está disponible.")
                    ]);
                }

                var estadoReagendada = await dbContext.EstadosCita
                    .FirstOrDefaultAsync(item => item.Nombre == RescheduledStateName && item.Activo, cancellationToken);

                var pagoSnapshot = citaPagoImpactService.Capture(cita);
                var estadoAnterior = cita.IdEstadoCita;
                cita.IdPrestador = idPrestador;
                cita.FechaInicio = request.FechaInicio;
                cita.FechaFin = request.FechaFin ?? request.FechaInicio.AddMinutes(cita.Servicio.DuracionMinutos);
                cita.FechaActualizacion = DateTime.Now;

                if (estadoReagendada is not null)
                {
                    cita.IdEstadoCita = estadoReagendada.IdEstadoCita;
                }

                AddHistorial(cita, estadoAnterior, cita.IdEstadoCita, request.Observacion ?? "Reserva reagendada por el cliente desde página pública.");
                await citaPagoImpactService.RegistrarReagendamientoAsync(
                    cita,
                    pagoSnapshot,
                    null,
                    request.Observacion ?? "Reserva reagendada por el cliente desde página pública.",
                    cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);
                await notificacionService.CrearPorCambioEstadoAsync(cita.IdCita, RescheduledStateName, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                var updated = await GetReservaDetalleQuery()
                    .FirstAsync(item => item.IdCita == cita.IdCita, cancellationToken);

                return ServiceResult<PublicReservaDto>.Success(ToPublicReservaDto(updated));
            },
            cancellationToken);
    }

    private IQueryable<Negocio> GetNegocioBySlugQuery(string slug)
    {
        var normalizedSlug = slug.Trim();

        return dbContext.Negocios
            .Include(negocio => negocio.Rubro)
            .Where(negocio => negocio.Slug == normalizedSlug && negocio.Activo);
    }

    private IQueryable<Cita> GetReservaDetalleQuery()
    {
        return dbContext.Citas
            .Include(item => item.Negocio)
            .Include(item => item.Cliente)
            .Include(item => item.Servicio)
            .Include(item => item.Prestador)
            .Include(item => item.EstadoCita);
    }

    private async Task<Cita?> GetEditablePublicReservaAsync(
        string slug,
        string codigo,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim();
        var normalizedCodigo = codigo.Trim();

        return await GetReservaDetalleQuery()
            .FirstOrDefaultAsync(
                item =>
                    item.Negocio.Slug == normalizedSlug &&
                    item.Negocio.Activo &&
                    item.Codigo == normalizedCodigo,
                cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateReservaAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateReservaPublicaRequest request,
        CancellationToken cancellationToken)
    {
        var errors = DataAnnotationsValidator.Validate(request).ToList();
        if (errors.Count > 0)
        {
            return errors;
        }

        if (!currentUser.IsAuthenticated &&
            string.IsNullOrWhiteSpace(request.Email) &&
            string.IsNullOrWhiteSpace(request.Telefono))
        {
            errors.Add(new ValidationError(
                nameof(CreateReservaPublicaRequest.Email),
                "Debes indicar un email o teléfono de contacto para crear la reserva."));
            return errors;
        }

        var servicio = await dbContext.Servicios
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.IdNegocio == idNegocio && item.IdServicio == request.IdServicio && item.Activo,
                cancellationToken);

        if (servicio is null)
        {
            errors.Add(new ValidationError(nameof(CreateReservaPublicaRequest.IdServicio), "El servicio indicado no existe o no está activo para este negocio."));
            return errors;
        }

        if (servicio.RequiereProfesional && !request.IdPrestador.HasValue)
        {
            errors.Add(new ValidationError(nameof(CreateReservaPublicaRequest.IdPrestador), "El servicio requiere seleccionar un prestador o recurso."));
        }

        if (request.IdPrestador.HasValue)
        {
            var prestadorServicioExists = await dbContext.PrestadorServicios.AnyAsync(
                relacion =>
                    relacion.IdNegocio == idNegocio &&
                    relacion.IdPrestador == request.IdPrestador.Value &&
                    relacion.IdServicio == servicio.IdServicio &&
                    relacion.Activo &&
                    relacion.Prestador.Activo,
                cancellationToken);

            if (!prestadorServicioExists)
            {
                errors.Add(new ValidationError(nameof(CreateReservaPublicaRequest.IdPrestador), "El prestador o recurso no está disponible para este servicio."));
            }
        }

        await ValidateCamposPersonalizadosAsync(idNegocio, servicio.IdServicio, request.CamposValor ?? [], errors, cancellationToken);

        if (errors.Count > 0)
        {
            return errors;
        }

        var fecha = DateOnly.FromDateTime(request.FechaInicio);
        var disponibilidad = await disponibilidadService.GetDisponibilidadAsync(
            idNegocio,
            new DisponibilidadQuery
            {
                IdServicio = servicio.IdServicio,
                IdPrestador = request.IdPrestador,
                Fecha = fecha
            },
            cancellationToken);

        if (!disponibilidad.Succeeded || disponibilidad.Data is null)
        {
            errors.AddRange(disponibilidad.ValidationErrors.Count > 0
                ? disponibilidad.ValidationErrors
                : disponibilidad.Errors.Select(error => new ValidationError(string.Empty, error)));

            return errors;
        }

        var hasSlot = disponibilidad.Data.Slots.Any(slot =>
            slot.FechaInicio == request.FechaInicio &&
            slot.IdPrestador == request.IdPrestador);

        if (!hasSlot)
        {
            errors.Add(new ValidationError(nameof(CreateReservaPublicaRequest.FechaInicio), "El horario seleccionado ya no está disponible."));
        }

        return errors;
    }

    private static ServiceResult<PublicReservaDto> ToPublicReservaFailure(
        ServiceResult<ClienteReservaDto> clienteResult)
    {
        return clienteResult.Status switch
        {
            ServiceResultStatus.Forbidden => ServiceResult<PublicReservaDto>.Forbidden(
                clienteResult.Errors.FirstOrDefault() ?? "No tienes permisos para resolver el cliente de la reserva."),
            ServiceResultStatus.NotFound => ServiceResult<PublicReservaDto>.NotFound(
                clienteResult.Errors.FirstOrDefault() ?? "No se pudo resolver el cliente de la reserva."),
            _ => ServiceResult<PublicReservaDto>.Validation(
                clienteResult.ValidationErrors.Count > 0
                    ? clienteResult.ValidationErrors
                    : clienteResult.Errors.Select(error => new ValidationError(string.Empty, error)))
        };
    }

    private async Task<List<ValidationError>> ValidateMaxCitasActivasAsync(
        int idNegocio,
        int idCliente,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var regla = await dbContext.ReglasReserva
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio, cancellationToken);
        var maxCitasActivas = regla?.MaxCitasActivasPorCliente ?? 1;

        var activeCount = await dbContext.Citas.CountAsync(
            cita =>
                cita.IdNegocio == idNegocio &&
                cita.IdCliente == idCliente &&
                !cita.EstadoCita.EsEstadoFinal,
            cancellationToken);

        if (activeCount >= maxCitasActivas)
        {
            errors.Add(new ValidationError(string.Empty, $"El cliente ya tiene el máximo de {maxCitasActivas} cita(s) activa(s) permitido por el negocio."));
        }

        return errors;
    }

    private async Task<List<ValidationError>> ValidatePublicClientActionAsync(
        Cita cita,
        string? email,
        string? telefono,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (!MatchesCliente(cita.Cliente, email, telefono))
        {
            errors.Add(new ValidationError(string.Empty, "Los datos de contacto no coinciden con la reserva."));
            return errors;
        }

        if (cita.EstadoCita.EsEstadoFinal)
        {
            errors.Add(new ValidationError(string.Empty, "La reserva ya está en un estado final."));
            return errors;
        }

        var regla = await dbContext.ReglasReserva
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == cita.IdNegocio, cancellationToken);

        if (regla?.PermiteCancelacionCliente == false)
        {
            errors.Add(new ValidationError(string.Empty, "El negocio no permite que el cliente cancele o reagende reservas."));
        }

        var horasLimite = regla?.HorasLimiteCancelacion ?? 6;
        if (DateTime.Now.AddHours(horasLimite) > cita.FechaInicio)
        {
            errors.Add(new ValidationError(string.Empty, $"La reserva solo puede modificarse hasta {horasLimite} horas antes."));
        }

        return errors;
    }

    private static bool MatchesCliente(Cliente cliente, string? email, string? telefono)
    {
        if (!string.IsNullOrWhiteSpace(email) &&
            !string.IsNullOrWhiteSpace(cliente.Email) &&
            string.Equals(cliente.Email.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(telefono) &&
            !string.IsNullOrWhiteSpace(cliente.Telefono) &&
            string.Equals(cliente.Telefono.Trim(), telefono.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task ValidateCamposPersonalizadosAsync(
        int idNegocio,
        int idServicio,
        IReadOnlyCollection<CitaCampoValorRequest> camposValor,
        List<ValidationError> errors,
        CancellationToken cancellationToken)
    {
        var duplicated = camposValor
            .GroupBy(campo => campo.IdCampoReserva)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicated is not null)
        {
            errors.Add(new ValidationError(nameof(CreateReservaPublicaRequest.CamposValor), "No puedes enviar el mismo campo personalizado más de una vez."));
            return;
        }

        var campos = await dbContext.CamposReserva
            .AsNoTracking()
            .Include(campo => campo.TipoCampo)
            .Where(campo =>
                campo.IdNegocio == idNegocio &&
                campo.Activo &&
                (!campo.IdServicio.HasValue || campo.IdServicio == idServicio))
            .ToArrayAsync(cancellationToken);
        var ids = camposValor.Select(campo => campo.IdCampoReserva).ToArray();
        var missing = ids.Where(id => campos.All(campo => campo.IdCampoReserva != id)).ToArray();

        if (missing.Length > 0)
        {
            errors.Add(new ValidationError(nameof(CreateReservaPublicaRequest.CamposValor), "Uno o más campos personalizados no existen o no están activos para este negocio."));
        }

        foreach (var obligatorio in campos.Where(campo => campo.Obligatorio))
        {
            var value = camposValor.FirstOrDefault(campo => campo.IdCampoReserva == obligatorio.IdCampoReserva)?.Valor;
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new ValidationError(nameof(CreateReservaPublicaRequest.CamposValor), $"El campo '{obligatorio.Etiqueta}' es obligatorio."));
            }
        }

        var selectFields = campos
            .Where(campo => campo.TipoCampo.Nombre == "Select")
            .ToArray();

        foreach (var campoValor in camposValor.Where(campo => !string.IsNullOrWhiteSpace(campo.Valor)))
        {
            var campo = selectFields.FirstOrDefault(item => item.IdCampoReserva == campoValor.IdCampoReserva);
            if (campo is null)
            {
                continue;
            }

            var optionExists = await dbContext.CampoReservaOpciones.AnyAsync(
                opcion =>
                    opcion.IdNegocio == idNegocio &&
                    opcion.IdCampoReserva == campo.IdCampoReserva &&
                    opcion.Valor == campoValor.Valor &&
                    opcion.Activo,
                cancellationToken);

            if (!optionExists)
            {
                errors.Add(new ValidationError(nameof(CreateReservaPublicaRequest.CamposValor), $"El valor del campo '{campo.Etiqueta}' no es una opción válida."));
            }
        }
    }

    private async Task<EstadoCita?> GetDefaultEstadoAsync(
        bool requierePagoAnticipado,
        bool requiereConfirmacionManual,
        CancellationToken cancellationToken)
    {
        var name = requierePagoAnticipado
            ? PaymentPendingStateName
            : requiereConfirmacionManual
                ? PendingStateName
                : ConfirmedStateName;

        return await dbContext.EstadosCita.FirstOrDefaultAsync(estado => estado.Nombre == name && estado.Activo, cancellationToken)
            ?? await dbContext.EstadosCita.FirstOrDefaultAsync(estado => estado.Activo, cancellationToken);
    }

    private async Task<string> GenerateCodigoAsync(Negocio negocio, CancellationToken cancellationToken)
    {
        var prefix = new string(negocio.Slug.Where(char.IsLetterOrDigit).Take(4).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = $"NEG{negocio.IdNegocio}";
        }

        var year = DateTime.Now.Year;
        var count = await dbContext.Citas.CountAsync(
            cita => cita.IdNegocio == negocio.IdNegocio && cita.FechaCreacion.Year == year,
            cancellationToken);

        return $"{prefix}-{year}-{count + 1:000000}";
    }

    private static void AddCampoValores(Cita cita, IReadOnlyCollection<CitaCampoValorRequest>? camposValor)
    {
        foreach (var campo in camposValor ?? [])
        {
            cita.CamposValor.Add(new CitaCampoValor
            {
                IdNegocio = cita.IdNegocio,
                IdCampoReserva = campo.IdCampoReserva,
                Valor = campo.Valor?.Trim()
            });
        }
    }

    private static void AddHistorial(
        Cita cita,
        int? idEstadoAnterior,
        int idEstadoNuevo,
        string? observacion)
    {
        cita.Historial.Add(new CitaHistorial
        {
            IdNegocio = cita.IdNegocio,
            IdEstadoAnterior = idEstadoAnterior,
            IdEstadoNuevo = idEstadoNuevo,
            Observacion = observacion?.Trim()
        });
    }

    private async Task<IReadOnlyDictionary<int, PublicReviewSummary>> GetPublicReviewSummariesAsync(
        IReadOnlyCollection<int> negocioIds,
        CancellationToken cancellationToken)
    {
        if (negocioIds.Count == 0)
        {
            return new Dictionary<int, PublicReviewSummary>();
        }

        var hiddenReviewBusinessIds = await dbContext.ConfiguracionesResenaNegocio
            .AsNoTracking()
            .Where(configuracion =>
                negocioIds.Contains(configuracion.IdNegocio) &&
                !configuracion.MostrarResenasPublicas)
            .Select(configuracion => configuracion.IdNegocio)
            .ToArrayAsync(cancellationToken);

        var hiddenSet = hiddenReviewBusinessIds.ToHashSet();
        var visibleBusinessIds = negocioIds
            .Where(idNegocio => !hiddenSet.Contains(idNegocio))
            .ToArray();

        if (visibleBusinessIds.Length == 0)
        {
            return new Dictionary<int, PublicReviewSummary>();
        }

        var ratingCounts = await dbContext.ResenasNegocio
            .AsNoTracking()
            .Where(resena =>
                visibleBusinessIds.Contains(resena.IdNegocio) &&
                resena.Activo &&
                resena.Estado == ApprovedReviewStateName &&
                resena.EsVisiblePublicamente)
            .GroupBy(resena => new { resena.IdNegocio, resena.Puntuacion })
            .Select(group => new
            {
                group.Key.IdNegocio,
                group.Key.Puntuacion,
                Cantidad = group.Count()
            })
            .ToArrayAsync(cancellationToken);

        return ratingCounts
            .GroupBy(item => item.IdNegocio)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var counts = group.ToDictionary(item => (int)item.Puntuacion, item => item.Cantidad);
                    var total = counts.Values.Sum();
                    var weightedTotal = counts.Sum(item => item.Key * item.Value);
                    var promedio = total == 0 ? 0m : Math.Round((decimal)weightedTotal / total, 2);
                    var distribution = BuildStarDistribution(counts, total);

                    return new PublicReviewSummary(promedio, total, total, distribution);
                });
    }

    private static IReadOnlyCollection<PublicNegocioEstrellaDto> BuildStarDistribution(
        IReadOnlyDictionary<int, int> counts,
        int total)
    {
        return Enumerable.Range(1, 5)
            .Select(puntuacion =>
            {
                counts.TryGetValue(puntuacion, out var cantidad);
                return new PublicNegocioEstrellaDto(
                    (byte)puntuacion,
                    cantidad,
                    Percentage(cantidad, total));
            })
            .ToArray();
    }

    private static IReadOnlyCollection<PublicNegocioEstrellaDto> EmptyStarDistribution()
    {
        return BuildStarDistribution(new Dictionary<int, int>(), total: 0);
    }

    private static decimal Percentage(int part, int total)
    {
        return total == 0 ? 0m : Math.Round(part * 100m / total, 2);
    }

    private static PublicNegocioDto ToPublicNegocioDto(Negocio negocio)
    {
        return new PublicNegocioDto(
            negocio.IdNegocio,
            negocio.Rubro.Nombre,
            negocio.Nombre,
            negocio.Slug,
            negocio.Descripcion,
            negocio.LogoUrl,
            negocio.Direccion,
            negocio.Telefono,
            negocio.Email);
    }

    private static PublicReglaReservaDto ToPublicReglaReservaDto(ReglaReserva? regla)
    {
        var now = DateTime.Now;
        var minHorasAnticipacion = regla?.MinHorasAnticipacion ?? DefaultMinHorasAnticipacion;
        var maxDiasAdelanto = regla?.MaxDiasAdelanto ?? DefaultMaxDiasAdelanto;
        var horasLimiteCancelacion = regla?.HorasLimiteCancelacion ?? DefaultHorasLimiteCancelacion;
        var maxCitasActivasPorCliente = regla?.MaxCitasActivasPorCliente ?? DefaultMaxCitasActivasPorCliente;

        return new PublicReglaReservaDto(
            minHorasAnticipacion,
            maxDiasAdelanto,
            regla?.PermiteCancelacionCliente ?? true,
            horasLimiteCancelacion,
            regla?.RequiereConfirmacionManual ?? false,
            regla?.PermiteSobreturnos ?? false,
            maxCitasActivasPorCliente,
            now.AddHours(minHorasAnticipacion),
            now.Date.AddDays(maxDiasAdelanto),
            regla?.FechaActualizacion);
    }

    private static PublicReservaDto ToPublicReservaDto(Cita cita)
    {
        return new PublicReservaDto(
            cita.IdCita,
            cita.IdNegocio,
            cita.Negocio.Nombre,
            cita.IdCliente,
            cita.Cliente.Nombre,
            cita.IdServicio,
            cita.Servicio.Nombre,
            cita.IdPrestador,
            cita.Prestador?.Nombre,
            cita.IdEstadoCita,
            cita.EstadoCita.Nombre,
            cita.Codigo,
            cita.FechaInicio,
            cita.FechaFin,
            cita.PrecioEstimado);
    }

    private sealed record PublicReviewSummary(
        decimal PromedioPuntuacion,
        int TotalResenas,
        int TotalPublicadas,
        IReadOnlyCollection<PublicNegocioEstrellaDto> DistribucionEstrellas);

    private static string? BuildNombreCliente(
        string? nombreCompleto,
        string? nombre,
        string? apellido,
        string? userName,
        string? email)
    {
        if (!string.IsNullOrWhiteSpace(nombreCompleto))
        {
            return nombreCompleto.Trim();
        }

        var fullName = string.Join(
            ' ',
            new[] { nombre, apellido }.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!.Trim()));

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return !string.IsNullOrWhiteSpace(userName)
            ? userName.Trim()
            : email?.Trim();
    }

    private static PublicCampoReservaDto ToPublicCampoReservaDto(CampoReserva campo)
    {
        return new PublicCampoReservaDto(
            campo.IdCampoReserva,
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
            campo.Opciones
                .Where(opcion => opcion.Activo)
                .OrderBy(opcion => opcion.Orden)
                .ThenBy(opcion => opcion.Etiqueta)
                .Select(opcion => new PublicCampoReservaOpcionDto(
                    opcion.IdCampoReservaOpcion,
                    opcion.Etiqueta,
                    opcion.Valor,
                    opcion.Orden))
                .ToArray());
    }
}
