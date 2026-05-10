using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.Notificaciones;
using TuCita.Infrastucture.Email;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Notificaciones;

public sealed class NotificacionService(
    ReservaFlowDbContext dbContext,
    IEmailSender emailSender,
    INotificacionEmailTemplateRenderer templateRenderer,
    IOptions<EmailOptions> emailOptions,
    IAuditoriaService auditoriaService) : INotificacionService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const string CanalEmailName = "Email";
    private const string EstadoPendienteName = "Pendiente";
    private const string EstadoEnviadaName = "Enviada";
    private const string EstadoErrorName = "Error";
    private const string TipoConfirmacionName = "ConfirmacionReserva";
    private const string TipoCancelacionName = "CancelacionReserva";
    private const string TipoReagendamientoName = "ReagendamientoReserva";
    private const string TipoRecordatorio24Name = "Recordatorio24Horas";
    private const string TipoRecordatorio2Name = "Recordatorio2Horas";
    private const string EstadoCitaConfirmadaName = "Confirmada";
    private const string EstadoCitaCanceladaName = "Cancelada";
    private const string EstadoCitaReagendadaName = "Reagendada";
    private const string ProcessingLockResource = "TuCita:Notificaciones:ProcesarPendientes";

    public async Task CrearPorCitaCreadaAsync(
        int idCita,
        CancellationToken cancellationToken)
    {
        var cita = await BaseCitaQuery()
            .FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);

        if (cita is null)
        {
            return;
        }

        await CrearNotificacionAsync(cita, TipoConfirmacionName, cancellationToken);
        await CrearRecordatoriosAsync(cita, cancellationToken);
    }

    public async Task CrearPorCambioEstadoAsync(
        int idCita,
        string estadoNuevo,
        CancellationToken cancellationToken)
    {
        var cita = await BaseCitaQuery()
            .FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);

        if (cita is null)
        {
            return;
        }

        if (estadoNuevo.Equals(EstadoCitaConfirmadaName, StringComparison.OrdinalIgnoreCase))
        {
            await CrearNotificacionAsync(cita, TipoConfirmacionName, cancellationToken);
            await CrearRecordatoriosAsync(cita, cancellationToken);
            return;
        }

        if (estadoNuevo.Equals(EstadoCitaCanceladaName, StringComparison.OrdinalIgnoreCase))
        {
            await CrearNotificacionAsync(cita, TipoCancelacionName, cancellationToken);
            await CancelarRecordatoriosPendientesAsync(cita, cancellationToken);
            return;
        }

        if (estadoNuevo.Equals(EstadoCitaReagendadaName, StringComparison.OrdinalIgnoreCase))
        {
            await CrearNotificacionAsync(cita, TipoReagendamientoName, cancellationToken);
            await CancelarRecordatoriosPendientesAsync(cita, cancellationToken);
            await CrearRecordatoriosAsync(cita, cancellationToken);
        }
    }

    public async Task<ServiceResult<ProcesarNotificacionesResultDto>> ProcesarPendientesAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        int maxNotificaciones,
        CancellationToken cancellationToken)
    {
        if (!await CanProcessAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<ProcesarNotificacionesResultDto>.Forbidden("No tienes acceso para procesar notificaciones.");
        }

        var emailConfigurationErrors = ValidateEmailConfiguration();
        if (emailConfigurationErrors.Count > 0)
        {
            return ServiceResult<ProcesarNotificacionesResultDto>.Validation(emailConfigurationErrors);
        }

        var lockAcquired = false;
        var estadoPendiente = await dbContext.EstadosNotificacion
            .FirstOrDefaultAsync(estado => estado.Nombre == EstadoPendienteName && estado.Activo, cancellationToken);

        try
        {
            lockAcquired = await TryAcquireProcessingLockAsync(cancellationToken);
            if (!lockAcquired)
            {
                return ServiceResult<ProcesarNotificacionesResultDto>.Validation([
                    new ValidationError(string.Empty, "Ya existe otro proceso enviando notificaciones pendientes.")
                ]);
            }

            var estadoEnviada = await dbContext.EstadosNotificacion
                .FirstOrDefaultAsync(estado => estado.Nombre == EstadoEnviadaName && estado.Activo, cancellationToken);
            var estadoError = await dbContext.EstadosNotificacion
                .FirstOrDefaultAsync(estado => estado.Nombre == EstadoErrorName && estado.Activo, cancellationToken);

            if (estadoPendiente is null || estadoEnviada is null || estadoError is null)
            {
                return ServiceResult<ProcesarNotificacionesResultDto>.Validation([
                    new ValidationError(string.Empty, "Faltan estados activos de notificación para procesar pendientes.")
                ]);
            }

            var now = DateTime.Now;
            var limit = Math.Clamp(maxNotificaciones, 1, 500);
            var query = dbContext.Notificaciones
                .Include(notificacion => notificacion.Negocio)
                .Include(notificacion => notificacion.Cita)
                    .ThenInclude(cita => cita!.Cliente)
                .Include(notificacion => notificacion.Cita)
                    .ThenInclude(cita => cita!.Servicio)
                .Include(notificacion => notificacion.Cita)
                    .ThenInclude(cita => cita!.Prestador)
                .Include(notificacion => notificacion.Cita)
                    .ThenInclude(cita => cita!.EstadoCita)
                .Include(notificacion => notificacion.Cita)
                    .ThenInclude(cita => cita!.Negocio)
                .Include(notificacion => notificacion.ResenaNegocio)
                .Include(notificacion => notificacion.TipoNotificacion)
                .Include(notificacion => notificacion.CanalNotificacion)
                .Include(notificacion => notificacion.EstadoNotificacion)
                .Where(notificacion =>
                    notificacion.IdEstadoNotificacion == estadoPendiente.IdEstadoNotificacion &&
                    (!notificacion.FechaProgramada.HasValue || notificacion.FechaProgramada <= now));

            if (idNegocio.HasValue)
            {
                query = query.Where(notificacion => notificacion.IdNegocio == idNegocio.Value);
            }
            else if (!currentUser.IsSuperAdmin)
            {
                query = query.Where(notificacion =>
                    notificacion.Negocio.NegocioUsuarios.Any(usuario =>
                        usuario.UserId == currentUser.UserId &&
                        usuario.Activo &&
                        (usuario.RolNegocio.Nombre == OwnerRoleName || usuario.RolNegocio.Nombre == AdminRoleName)));
            }

            var pendientes = await query
                .OrderBy(notificacion => notificacion.FechaProgramada ?? notificacion.FechaCreacion)
                .Take(limit)
                .ToArrayAsync(cancellationToken);

            foreach (var notificacion in pendientes)
            {
                var previousSnapshot = ToAuditSnapshot(
                    notificacion,
                    notificacion.EstadoNotificacion.Nombre);

                if (string.IsNullOrWhiteSpace(notificacion.Destinatario))
                {
                    notificacion.IdEstadoNotificacion = estadoError.IdEstadoNotificacion;
                    notificacion.Error = "No existe destinatario para enviar la notificación.";
                    await RegistrarAuditoriaProcesamientoAsync(
                        currentUser,
                        notificacion,
                        previousSnapshot,
                        estadoError.Nombre,
                        "MarcarError",
                        "Notificación marcada con error por no tener destinatario.",
                        cancellationToken);
                    continue;
                }

                if (!notificacion.CanalNotificacion.Nombre.Equals(CanalEmailName, StringComparison.OrdinalIgnoreCase))
                {
                    notificacion.IdEstadoNotificacion = estadoError.IdEstadoNotificacion;
                    notificacion.Error = $"Canal de notificación no soportado para envío automático: {notificacion.CanalNotificacion.Nombre}.";
                    await RegistrarAuditoriaProcesamientoAsync(
                        currentUser,
                        notificacion,
                        previousSnapshot,
                        estadoError.Nombre,
                        "MarcarError",
                        $"Notificación marcada con error por canal no soportado: {notificacion.CanalNotificacion.Nombre}.",
                        cancellationToken);
                    continue;
                }

                if (!IsValidEmail(notificacion.Destinatario))
                {
                    notificacion.IdEstadoNotificacion = estadoError.IdEstadoNotificacion;
                    notificacion.Error = "El destinatario no es un correo electrónico válido.";
                    await RegistrarAuditoriaProcesamientoAsync(
                        currentUser,
                        notificacion,
                        previousSnapshot,
                        estadoError.Nombre,
                        "MarcarError",
                        "Notificación marcada con error por destinatario inválido.",
                        cancellationToken);
                    continue;
                }

                try
                {
                    var template = templateRenderer.Render(notificacion);
                    await emailSender.SendAsync(
                        new EmailMessage(
                            notificacion.Destinatario.Trim(),
                            template.Subject,
                            template.HtmlBody,
                            template.TextBody),
                        cancellationToken);

                    notificacion.Asunto = template.Subject;
                    notificacion.Mensaje = template.TextBody;
                    notificacion.IdEstadoNotificacion = estadoEnviada.IdEstadoNotificacion;
                    notificacion.FechaEnvio = DateTime.Now;
                    notificacion.Error = null;
                    await RegistrarAuditoriaProcesamientoAsync(
                        currentUser,
                        notificacion,
                        previousSnapshot,
                        estadoEnviada.Nombre,
                        "Enviar",
                        $"Notificación enviada a {notificacion.Destinatario.Trim()}.",
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    notificacion.IdEstadoNotificacion = estadoError.IdEstadoNotificacion;
                    notificacion.Error = TrimToMax(exception.Message, 1000);
                    await RegistrarAuditoriaProcesamientoAsync(
                        currentUser,
                        notificacion,
                        previousSnapshot,
                        estadoError.Nombre,
                        "MarcarError",
                        $"Notificación marcada con error al enviar: {notificacion.Error}",
                        cancellationToken);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var ids = pendientes.Select(notificacion => notificacion.IdNotificacion).ToArray();
            var processed = await dbContext.Notificaciones
                .AsNoTracking()
                .Include(notificacion => notificacion.TipoNotificacion)
                .Include(notificacion => notificacion.CanalNotificacion)
                .Include(notificacion => notificacion.EstadoNotificacion)
                .Where(notificacion => ids.Contains(notificacion.IdNotificacion))
                .OrderBy(notificacion => notificacion.IdNotificacion)
                .ToArrayAsync(cancellationToken);

            var result = new ProcesarNotificacionesResultDto(
                processed.Length,
                processed.Count(notificacion => notificacion.EstadoNotificacion.Nombre == EstadoEnviadaName),
                processed.Count(notificacion => notificacion.EstadoNotificacion.Nombre == EstadoErrorName),
                processed.Select(ToDto).ToArray());

            return ServiceResult<ProcesarNotificacionesResultDto>.Success(result);
        }
        finally
        {
            if (lockAcquired)
            {
                await ReleaseProcessingLockAsync(CancellationToken.None);
            }
        }
    }

    private IQueryable<Cita> BaseCitaQuery()
    {
        return dbContext.Citas
            .Include(cita => cita.Negocio)
            .Include(cita => cita.Cliente)
            .Include(cita => cita.Servicio)
            .Include(cita => cita.Prestador)
            .Include(cita => cita.EstadoCita);
    }

    private async Task<bool> TryAcquireProcessingLockAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            var result = new SqlParameter("@Result", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            var resource = new SqlParameter("@ResourceParam", SqlDbType.NVarChar, 255)
            {
                Value = ProcessingLockResource
            };
            var timeout = new SqlParameter("@LockTimeout", SqlDbType.Int)
            {
                Value = 10_000
            };

            await dbContext.Database.ExecuteSqlRawAsync(
                "EXEC @Result = sp_getapplock @Resource = @ResourceParam, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = @LockTimeout;",
                new object[] { result, resource, timeout },
                cancellationToken);

            var acquired = Convert.ToInt32(result.Value) >= 0;
            if (!acquired)
            {
                await dbContext.Database.CloseConnectionAsync();
            }

            return acquired;
        }
        catch
        {
            await dbContext.Database.CloseConnectionAsync();
            throw;
        }
    }

    private async Task ReleaseProcessingLockAsync(CancellationToken cancellationToken)
    {
        try
        {
            var resource = new SqlParameter("@ResourceParam", SqlDbType.NVarChar, 255)
            {
                Value = ProcessingLockResource
            };

            await dbContext.Database.ExecuteSqlRawAsync(
                "EXEC sp_releaseapplock @Resource = @ResourceParam, @LockOwner = 'Session';",
                new object[] { resource },
                cancellationToken);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private async Task CrearNotificacionAsync(
        Cita cita,
        string tipoNombre,
        CancellationToken cancellationToken)
    {
        var metadata = await GetMetadataAsync(tipoNombre, cancellationToken);
        if (metadata is null)
        {
            return;
        }

        var destinatario = GetDestinatario(cita);
        dbContext.Notificaciones.Add(new Notificacion
        {
            IdNegocio = cita.IdNegocio,
            IdCita = cita.IdCita,
            IdTipoNotificacion = metadata.Value.IdTipoNotificacion,
            IdCanalNotificacion = metadata.Value.IdCanalNotificacion,
            IdEstadoNotificacion = metadata.Value.IdEstadoNotificacion,
            Destinatario = destinatario,
            Asunto = GetAsunto(cita, tipoNombre),
            Mensaje = GetMensaje(cita, tipoNombre),
            FechaProgramada = DateTime.Now
        });
    }

    private async Task CrearRecordatoriosAsync(Cita cita, CancellationToken cancellationToken)
    {
        await CrearRecordatorioAsync(cita, TipoRecordatorio24Name, cita.FechaInicio.AddHours(-24), cancellationToken);
        await CrearRecordatorioAsync(cita, TipoRecordatorio2Name, cita.FechaInicio.AddHours(-2), cancellationToken);
    }

    private async Task CrearRecordatorioAsync(
        Cita cita,
        string tipoNombre,
        DateTime fechaProgramada,
        CancellationToken cancellationToken)
    {
        if (fechaProgramada <= DateTime.Now)
        {
            return;
        }

        var metadata = await GetMetadataAsync(tipoNombre, cancellationToken);
        if (metadata is null)
        {
            return;
        }

        dbContext.Notificaciones.Add(new Notificacion
        {
            IdNegocio = cita.IdNegocio,
            IdCita = cita.IdCita,
            IdTipoNotificacion = metadata.Value.IdTipoNotificacion,
            IdCanalNotificacion = metadata.Value.IdCanalNotificacion,
            IdEstadoNotificacion = metadata.Value.IdEstadoNotificacion,
            Destinatario = GetDestinatario(cita),
            Asunto = GetAsunto(cita, tipoNombre),
            Mensaje = GetMensaje(cita, tipoNombre),
            FechaProgramada = fechaProgramada
        });
    }

    private async Task CancelarRecordatoriosPendientesAsync(Cita cita, CancellationToken cancellationToken)
    {
        var estadoPendiente = await dbContext.EstadosNotificacion
            .FirstOrDefaultAsync(estado => estado.Nombre == EstadoPendienteName && estado.Activo, cancellationToken);
        var estadoCancelada = await dbContext.EstadosNotificacion
            .FirstOrDefaultAsync(estado => estado.Nombre == "Cancelada" && estado.Activo, cancellationToken);

        if (estadoPendiente is null || estadoCancelada is null)
        {
            return;
        }

        var recordatorioTypes = new[] { TipoRecordatorio24Name, TipoRecordatorio2Name };
        var pendientes = await dbContext.Notificaciones
            .Include(notificacion => notificacion.TipoNotificacion)
            .Where(notificacion =>
                notificacion.IdNegocio == cita.IdNegocio &&
                notificacion.IdCita == cita.IdCita &&
                notificacion.IdEstadoNotificacion == estadoPendiente.IdEstadoNotificacion &&
                recordatorioTypes.Contains(notificacion.TipoNotificacion.Nombre))
            .ToArrayAsync(cancellationToken);

        foreach (var notificacion in pendientes)
        {
            notificacion.IdEstadoNotificacion = estadoCancelada.IdEstadoNotificacion;
        }
    }

    private async Task<NotificationMetadata?> GetMetadataAsync(
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
            .FirstOrDefaultAsync(item => item.Nombre == EstadoPendienteName && item.Activo, cancellationToken);

        return tipo is null || canal is null || estado is null
            ? null
            : new NotificationMetadata(tipo.IdTipoNotificacion, canal.IdCanalNotificacion, estado.IdEstadoNotificacion);
    }

    private async Task<bool> CanProcessAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        if (!idNegocio.HasValue)
        {
            return currentUser.IsAuthenticated;
        }

        return await dbContext.NegocioUsuarios.AnyAsync(
            item =>
                item.IdNegocio == idNegocio.Value &&
                item.UserId == currentUser.UserId &&
                item.Activo &&
                (item.RolNegocio.Nombre == OwnerRoleName || item.RolNegocio.Nombre == AdminRoleName),
            cancellationToken);
    }

    private static string GetDestinatario(Cita cita)
    {
        return !string.IsNullOrWhiteSpace(cita.Cliente.Email)
            ? cita.Cliente.Email
            : cita.Cliente.Telefono ?? string.Empty;
    }

    private static string GetAsunto(Cita cita, string tipoNombre)
    {
        return tipoNombre switch
        {
            TipoCancelacionName => $"Reserva cancelada en {cita.Negocio.Nombre}",
            TipoReagendamientoName => $"Reserva reagendada en {cita.Negocio.Nombre}",
            TipoRecordatorio24Name => $"Recordatorio de reserva en {cita.Negocio.Nombre}",
            TipoRecordatorio2Name => $"Tu reserva es pronto en {cita.Negocio.Nombre}",
            _ => $"Reserva en {cita.Negocio.Nombre}"
        };
    }

    private static string GetMensaje(Cita cita, string tipoNombre)
    {
        var prestador = cita.Prestador is null ? string.Empty : $" con {cita.Prestador.Nombre}";
        var fecha = cita.FechaInicio.ToString("dd/MM/yyyy HH:mm");

        return tipoNombre switch
        {
            TipoCancelacionName => $"Tu reserva {cita.Codigo} para {cita.Servicio.Nombre}{prestador} el {fecha} fue cancelada.",
            TipoReagendamientoName => $"Tu reserva {cita.Codigo} fue reagendada para {cita.Servicio.Nombre}{prestador} el {fecha}.",
            TipoRecordatorio24Name => $"Recuerda tu reserva {cita.Codigo} para {cita.Servicio.Nombre}{prestador} mañana a las {cita.FechaInicio:HH:mm}.",
            TipoRecordatorio2Name => $"Tu reserva {cita.Codigo} para {cita.Servicio.Nombre}{prestador} comienza a las {cita.FechaInicio:HH:mm}.",
            _ => $"Tu reserva {cita.Codigo} para {cita.Servicio.Nombre}{prestador} el {fecha} quedó registrada con estado {cita.EstadoCita.Nombre}."
        };
    }

    private static NotificacionDto ToDto(Notificacion notificacion)
    {
        return new NotificacionDto(
            notificacion.IdNotificacion,
            notificacion.IdNegocio,
            notificacion.IdCita,
            notificacion.TipoNotificacion.Nombre,
            notificacion.CanalNotificacion.Nombre,
            notificacion.EstadoNotificacion.Nombre,
            notificacion.Destinatario,
            notificacion.Asunto,
            notificacion.Mensaje,
            notificacion.FechaProgramada,
            notificacion.FechaEnvio,
            notificacion.Error,
            notificacion.FechaCreacion);
    }

    private async Task RegistrarAuditoriaProcesamientoAsync(
        CurrentUserContext currentUser,
        Notificacion notificacion,
        object previousSnapshot,
        string estadoNuevo,
        string accion,
        string descripcion,
        CancellationToken cancellationToken)
    {
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                notificacion.IdNegocio,
                "Notificaciones",
                accion,
                nameof(Notificacion),
                notificacion.IdNotificacion.ToString(),
                descripcion,
                previousSnapshot,
                ToAuditSnapshot(notificacion, estadoNuevo),
                new
                {
                    notificacion.IdCita,
                    Tipo = notificacion.TipoNotificacion.Nombre,
                    Canal = notificacion.CanalNotificacion.Nombre
                }),
            cancellationToken);
    }

    private static object ToAuditSnapshot(Notificacion notificacion, string estado)
    {
        return new
        {
            notificacion.IdNotificacion,
            notificacion.IdNegocio,
            notificacion.IdCita,
            Tipo = notificacion.TipoNotificacion.Nombre,
            Canal = notificacion.CanalNotificacion.Nombre,
            Estado = estado,
            notificacion.Destinatario,
            notificacion.Asunto,
            notificacion.FechaProgramada,
            notificacion.FechaEnvio,
            notificacion.Error
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new System.Net.Mail.MailAddress(email);
            return address.Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string TrimToMax(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private IReadOnlyCollection<ValidationError> ValidateEmailConfiguration()
    {
        var config = emailOptions.Value;
        var errors = new List<ValidationError>();

        if (!config.Enabled)
        {
            errors.Add(new ValidationError("Email.Enabled", "El envío de correos está deshabilitado."));
        }

        if (string.IsNullOrWhiteSpace(config.Host))
        {
            errors.Add(new ValidationError("Email.Host", "El host SMTP no está configurado."));
        }

        if (config.Port <= 0)
        {
            errors.Add(new ValidationError("Email.Port", "El puerto SMTP debe ser mayor a cero."));
        }

        if (string.IsNullOrWhiteSpace(config.UserName))
        {
            errors.Add(new ValidationError("Email.UserName", "El correo SMTP no está configurado."));
        }

        if (string.IsNullOrWhiteSpace(config.Password))
        {
            errors.Add(new ValidationError("Email.Password", "La clave SMTP no está configurada."));
        }

        return errors;
    }

    private readonly record struct NotificationMetadata(
        int IdTipoNotificacion,
        int IdCanalNotificacion,
        int IdEstadoNotificacion);
}
