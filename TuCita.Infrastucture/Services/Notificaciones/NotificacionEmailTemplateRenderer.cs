using System.Globalization;
using System.Net;
using TuCita.Infrastucture.Entities;

namespace TuCita.Infrastucture.Notificaciones;

public sealed class NotificacionEmailTemplateRenderer : INotificacionEmailTemplateRenderer
{
    private const string TipoConfirmacionName = "ConfirmacionReserva";
    private const string TipoCancelacionName = "CancelacionReserva";
    private const string TipoReagendamientoName = "ReagendamientoReserva";
    private const string TipoRecordatorio24Name = "Recordatorio24Horas";
    private const string TipoRecordatorio2Name = "Recordatorio2Horas";
    private const string TipoPostAtencionName = "PostAtencion";
    private const string TipoInvitacionNegocioName = "InvitacionNegocio";
    private const string TipoNuevaResenaNegocioName = "NuevaResenaNegocio";
    private const string TipoAlertaResenaNegocioName = "AlertaResenaNegocio";
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-CL");

    public NotificacionEmailTemplate Render(Notificacion notificacion)
    {
        return notificacion.TipoNotificacion.Nombre switch
        {
            TipoCancelacionName => RenderCancelacion(notificacion),
            TipoReagendamientoName => RenderReagendamiento(notificacion),
            TipoRecordatorio24Name => RenderRecordatorio24(notificacion),
            TipoRecordatorio2Name => RenderRecordatorio2(notificacion),
            TipoPostAtencionName => RenderPostAtencion(notificacion),
            TipoInvitacionNegocioName => RenderInvitacionNegocio(notificacion),
            TipoNuevaResenaNegocioName => RenderResenaNegocio(notificacion, esAlerta: false),
            TipoAlertaResenaNegocioName => RenderResenaNegocio(notificacion, esAlerta: true),
            _ => RenderConfirmacion(notificacion)
        };
    }

    private static NotificacionEmailTemplate RenderConfirmacion(Notificacion notificacion)
    {
        var context = BuildContext(notificacion);
        var subject = $"Reserva registrada en {context.Negocio}";
        var title = "Tu reserva fue registrada";
        var intro = $"Hola {context.Cliente}, recibimos tu reserva en {context.Negocio}.";
        var note = $"Estado actual: {context.EstadoCita}. Te avisaremos si el negocio realiza algún cambio.";

        return BuildTemplate(notificacion, subject, title, intro, note, "#2563eb");
    }

    private static NotificacionEmailTemplate RenderCancelacion(Notificacion notificacion)
    {
        var context = BuildContext(notificacion);
        var subject = $"Reserva cancelada en {context.Negocio}";
        var title = "Tu reserva fue cancelada";
        var intro = $"Hola {context.Cliente}, tu reserva {context.Codigo} fue cancelada.";
        var note = "Si necesitas una nueva hora, puedes volver a reservar desde la página del negocio.";

        return BuildTemplate(notificacion, subject, title, intro, note, "#dc2626");
    }

    private static NotificacionEmailTemplate RenderReagendamiento(Notificacion notificacion)
    {
        var context = BuildContext(notificacion);
        var subject = $"Reserva reagendada en {context.Negocio}";
        var title = "Tu reserva fue reagendada";
        var intro = $"Hola {context.Cliente}, tu reserva {context.Codigo} tiene una nueva fecha.";
        var note = "Revisa los datos actualizados de tu cita.";

        return BuildTemplate(notificacion, subject, title, intro, note, "#7c3aed");
    }

    private static NotificacionEmailTemplate RenderRecordatorio24(Notificacion notificacion)
    {
        var context = BuildContext(notificacion);
        var subject = $"Recordatorio de reserva en {context.Negocio}";
        var title = "Tu reserva es mañana";
        var intro = $"Hola {context.Cliente}, te recordamos tu reserva en {context.Negocio}.";
        var note = "Si no puedes asistir, contacta al negocio con anticipación.";

        return BuildTemplate(notificacion, subject, title, intro, note, "#0f766e");
    }

    private static NotificacionEmailTemplate RenderRecordatorio2(Notificacion notificacion)
    {
        var context = BuildContext(notificacion);
        var subject = $"Tu reserva es pronto en {context.Negocio}";
        var title = "Tu reserva comienza pronto";
        var intro = $"Hola {context.Cliente}, tu reserva comienza a las {context.HoraInicio}.";
        var note = "Te recomendamos llegar unos minutos antes.";

        return BuildTemplate(notificacion, subject, title, intro, note, "#ea580c");
    }

    private static NotificacionEmailTemplate RenderPostAtencion(Notificacion notificacion)
    {
        var context = BuildContext(notificacion);
        var subject = $"Gracias por visitar {context.Negocio}";
        var title = "Gracias por tu visita";
        var intro = $"Hola {context.Cliente}, gracias por asistir a tu reserva en {context.Negocio}.";
        var note = "Tu opinión ayuda al negocio a mejorar la atención y también orienta a otros clientes.";
        var actionUrl = string.IsNullOrWhiteSpace(notificacion.Mensaje) ? null : notificacion.Mensaje.Trim();

        return BuildTemplate(notificacion, subject, title, intro, note, "#16a34a", actionUrl, "Calificar atención");
    }

    private static NotificacionEmailTemplate RenderInvitacionNegocio(Notificacion notificacion)
    {
        var negocio = ValueOrDefault(notificacion.Negocio?.Nombre, "TuCita");
        var link = ValueOrDefault(notificacion.Mensaje, "#");
        var subject = $"Invitación para unirte a {negocio}";
        var title = "Tienes una invitación de negocio";
        var intro = $"Has sido invitado a unirte a {negocio} en TuCita.";
        var note = "Este enlace es personal, de un solo uso y puede expirar. Si no esperabas esta invitación, puedes ignorar este correo.";
        var textBody = string.Join(
            Environment.NewLine,
            title,
            string.Empty,
            intro,
            string.Empty,
            $"Negocio: {negocio}",
            $"Correo invitado: {notificacion.Destinatario}",
            $"Aceptar invitación: {link}",
            string.Empty,
            note);

        var htmlBody = $"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{Html(subject)}</title>
            </head>
            <body style="margin:0;background:#f6f7fb;font-family:Arial,Helvetica,sans-serif;color:#172033;">
              <div style="max-width:640px;margin:0 auto;padding:28px 16px;">
                <div style="background:#ffffff;border:1px solid #e6e8ef;border-radius:10px;overflow:hidden;">
                  <div style="height:6px;background:#4f46e5;"></div>
                  <div style="padding:28px;">
                    <p style="margin:0 0 8px;color:#64748b;font-size:13px;">TuCita</p>
                    <h1 style="margin:0 0 16px;font-size:24px;line-height:1.25;color:#111827;">{Html(title)}</h1>
                    <p style="margin:0 0 22px;font-size:16px;line-height:1.55;">{Html(intro)}</p>
                    <table role="presentation" style="width:100%;border-collapse:collapse;background:#f8fafc;border-radius:8px;overflow:hidden;">
                      {Row("Negocio", negocio)}
                      {Row("Correo invitado", notificacion.Destinatario)}
                    </table>
                    <div style="margin:26px 0 0;">
                      <a href="{Html(link)}" style="display:inline-block;background:#4f46e5;color:#ffffff;text-decoration:none;font-weight:700;font-size:15px;padding:12px 18px;border-radius:8px;">Aceptar invitación</a>
                    </div>
                    <p style="margin:22px 0 0;font-size:14px;line-height:1.55;color:#475569;">{Html(note)}</p>
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;

        return new NotificacionEmailTemplate(subject, htmlBody, textBody);
    }

    private static NotificacionEmailTemplate RenderResenaNegocio(Notificacion notificacion, bool esAlerta)
    {
        var context = BuildReviewContext(notificacion);
        var accentColor = esAlerta ? "#dc2626" : "#2563eb";
        var subject = esAlerta
            ? $"Alerta operativa: reseña baja en {context.Negocio}"
            : $"Nueva reseña recibida en {context.Negocio}";
        var title = esAlerta ? "Revisa una experiencia crítica" : "Nueva reseña recibida";
        var intro = esAlerta
            ? $"Una atención fue calificada con {context.Puntuacion} y quedó marcada como alerta operativa."
            : $"El negocio recibió una nueva reseña con puntuación {context.Puntuacion}.";
        var note = esAlerta
            ? "Revisa el caso con prioridad, responde si corresponde y coordina una acción interna para mejorar la experiencia."
            : "Puedes revisar, moderar o responder esta reseña desde el panel del negocio.";

        var textBody = string.Join(
            Environment.NewLine,
            title,
            string.Empty,
            intro,
            string.Empty,
            $"Negocio: {context.Negocio}",
            $"Código cita: {context.Codigo}",
            $"Cliente: {context.Cliente}",
            $"Servicio: {context.Servicio}",
            $"Prestador: {context.Prestador}",
            $"Puntuación: {context.Puntuacion}",
            $"Estado: {context.Estado}",
            $"Comentario: {context.Comentario}",
            string.Empty,
            note);

        var htmlBody = $"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{Html(subject)}</title>
            </head>
            <body style="margin:0;background:#f6f7fb;font-family:Arial,Helvetica,sans-serif;color:#172033;">
              <div style="max-width:640px;margin:0 auto;padding:28px 16px;">
                <div style="background:#ffffff;border:1px solid #e6e8ef;border-radius:10px;overflow:hidden;">
                  <div style="height:6px;background:{accentColor};"></div>
                  <div style="padding:28px;">
                    <p style="margin:0 0 8px;color:#64748b;font-size:13px;">{Html(context.Negocio)}</p>
                    <h1 style="margin:0 0 16px;font-size:24px;line-height:1.25;color:#111827;">{Html(title)}</h1>
                    <p style="margin:0 0 22px;font-size:16px;line-height:1.55;">{Html(intro)}</p>
                    <table role="presentation" style="width:100%;border-collapse:collapse;background:#f8fafc;border-radius:8px;overflow:hidden;">
                      {Row("Código cita", context.Codigo)}
                      {Row("Cliente", context.Cliente)}
                      {Row("Servicio", context.Servicio)}
                      {Row("Prestador", context.Prestador)}
                      {Row("Puntuación", context.Puntuacion)}
                      {Row("Estado", context.Estado)}
                      {Row("Comentario", context.Comentario)}
                    </table>
                    <p style="margin:22px 0 0;font-size:14px;line-height:1.55;color:#475569;">{Html(note)}</p>
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;

        return new NotificacionEmailTemplate(subject, htmlBody, textBody);
    }

    private static NotificacionEmailTemplate BuildTemplate(
        Notificacion notificacion,
        string subject,
        string title,
        string intro,
        string note,
        string accentColor,
        string? actionUrl = null,
        string? actionLabel = null)
    {
        var context = BuildContext(notificacion);
        var hasAction = !string.IsNullOrWhiteSpace(actionUrl) && !string.IsNullOrWhiteSpace(actionLabel);
        var textBody = string.Join(
            Environment.NewLine,
            title,
            string.Empty,
            intro,
            string.Empty,
            $"Código: {context.Codigo}",
            $"Negocio: {context.Negocio}",
            $"Servicio: {context.Servicio}",
            $"Prestador: {context.Prestador}",
            $"Fecha: {context.Fecha}",
            $"Horario: {context.HoraInicio} - {context.HoraFin}",
            $"Dirección: {context.Direccion}",
            hasAction ? $"{actionLabel}: {actionUrl}" : string.Empty,
            string.Empty,
            note);

        var htmlBody = $"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{Html(subject)}</title>
            </head>
            <body style="margin:0;background:#f6f7fb;font-family:Arial,Helvetica,sans-serif;color:#172033;">
              <div style="max-width:640px;margin:0 auto;padding:28px 16px;">
                <div style="background:#ffffff;border:1px solid #e6e8ef;border-radius:10px;overflow:hidden;">
                  <div style="height:6px;background:{accentColor};"></div>
                  <div style="padding:28px;">
                    <p style="margin:0 0 8px;color:#64748b;font-size:13px;">{Html(context.Negocio)}</p>
                    <h1 style="margin:0 0 16px;font-size:24px;line-height:1.25;color:#111827;">{Html(title)}</h1>
                    <p style="margin:0 0 22px;font-size:16px;line-height:1.55;">{Html(intro)}</p>
                    <table role="presentation" style="width:100%;border-collapse:collapse;background:#f8fafc;border-radius:8px;overflow:hidden;">
                      {Row("Código", context.Codigo)}
                      {Row("Servicio", context.Servicio)}
                      {Row("Prestador", context.Prestador)}
                      {Row("Fecha", context.Fecha)}
                      {Row("Horario", $"{context.HoraInicio} - {context.HoraFin}")}
                      {Row("Dirección", context.Direccion)}
                    </table>
                    {(hasAction ? $"""
                    <div style="margin:26px 0 0;">
                      <a href="{Html(actionUrl!)}" style="display:inline-block;background:{accentColor};color:#ffffff;text-decoration:none;font-weight:700;font-size:15px;padding:12px 18px;border-radius:8px;">{Html(actionLabel!)}</a>
                    </div>
                    """ : string.Empty)}
                    <p style="margin:22px 0 0;font-size:14px;line-height:1.55;color:#475569;">{Html(note)}</p>
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;

        return new NotificacionEmailTemplate(subject, htmlBody, textBody);
    }

    private static TemplateContext BuildContext(Notificacion notificacion)
    {
        var cita = notificacion.Cita;
        var negocio = cita?.Negocio ?? notificacion.Negocio;
        var cliente = cita?.Cliente;

        return new TemplateContext(
            Negocio: ValueOrDefault(negocio?.Nombre, "TuCita"),
            Cliente: ValueOrDefault(cliente?.Nombre, "cliente"),
            Codigo: ValueOrDefault(cita?.Codigo, $"NOT-{notificacion.IdNotificacion}"),
            Servicio: ValueOrDefault(cita?.Servicio?.Nombre, "Servicio no especificado"),
            Prestador: ValueOrDefault(cita?.Prestador?.Nombre, "Por asignar"),
            Fecha: cita is null ? "Fecha no especificada" : cita.FechaInicio.ToString("dddd dd/MM/yyyy", Culture),
            HoraInicio: cita is null ? "--:--" : cita.FechaInicio.ToString("HH:mm", Culture),
            HoraFin: cita is null ? "--:--" : cita.FechaFin.ToString("HH:mm", Culture),
            Direccion: ValueOrDefault(negocio?.Direccion, "Dirección no especificada"),
            EstadoCita: ValueOrDefault(cita?.EstadoCita?.Nombre, "Pendiente"));
    }

    private static ReviewTemplateContext BuildReviewContext(Notificacion notificacion)
    {
        var resena = notificacion.ResenaNegocio;
        var cita = notificacion.Cita;
        var negocio = notificacion.Negocio ?? cita?.Negocio;

        return new ReviewTemplateContext(
            Negocio: ValueOrDefault(negocio?.Nombre, "TuCita"),
            Codigo: ValueOrDefault(cita?.Codigo, $"RES-{resena?.IdResenaNegocio ?? notificacion.IdNotificacion}"),
            Cliente: ValueOrDefault(resena?.ClienteNombreSnapshot ?? cita?.Cliente?.Nombre, "Cliente"),
            Servicio: ValueOrDefault(resena?.ServicioNombreSnapshot ?? cita?.Servicio?.Nombre, "Servicio"),
            Prestador: ValueOrDefault(resena?.PrestadorNombreSnapshot ?? cita?.Prestador?.Nombre, "Sin prestador"),
            Puntuacion: resena is null ? "Sin puntuación" : $"{resena.Puntuacion.ToString(CultureInfo.InvariantCulture)}/5",
            Estado: ValueOrDefault(resena?.Estado, "Pendiente"),
            Comentario: ValueOrDefault(resena?.Comentario, "Sin comentario"));
    }

    private static string Row(string label, string value)
    {
        return $"""
            <tr>
              <td style="padding:12px 14px;border-bottom:1px solid #e6e8ef;color:#64748b;font-size:13px;width:34%;">{Html(label)}</td>
              <td style="padding:12px 14px;border-bottom:1px solid #e6e8ef;color:#111827;font-size:14px;font-weight:600;">{Html(value)}</td>
            </tr>
            """;
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string ValueOrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private sealed record TemplateContext(
        string Negocio,
        string Cliente,
        string Codigo,
        string Servicio,
        string Prestador,
        string Fecha,
        string HoraInicio,
        string HoraFin,
        string Direccion,
        string EstadoCita);

    private sealed record ReviewTemplateContext(
        string Negocio,
        string Codigo,
        string Cliente,
        string Servicio,
        string Prestador,
        string Puntuacion,
        string Estado,
        string Comentario);
}
