using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Options;

namespace TuCita.Infrastucture.Email;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var config = options.Value;
        ValidateConfiguration(config);

        var fromEmail = string.IsNullOrWhiteSpace(config.FromEmail)
            ? config.UserName
            : config.FromEmail.Trim();

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, config.FromName),
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        mailMessage.To.Add(new MailAddress(message.To));
        mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            message.TextBody,
            Encoding.UTF8,
            MediaTypeNames.Text.Plain));
        mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            message.HtmlBody,
            Encoding.UTF8,
            MediaTypeNames.Text.Html));

        using var smtpClient = new SmtpClient(config.Host, config.Port)
        {
            EnableSsl = config.EnableSsl,
            Credentials = new NetworkCredential(config.UserName, config.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = Math.Max(config.TimeoutSeconds, 1) * 1000
        };

        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
    }

    private static void ValidateConfiguration(EmailOptions config)
    {
        if (!config.Enabled)
        {
            throw new InvalidOperationException("El envío de correos está deshabilitado en la configuración.");
        }

        if (string.IsNullOrWhiteSpace(config.Host))
        {
            throw new InvalidOperationException("Email:Host no está configurado.");
        }

        if (config.Port <= 0)
        {
            throw new InvalidOperationException("Email:Port debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(config.UserName))
        {
            throw new InvalidOperationException("Email:UserName no está configurado.");
        }

        if (string.IsNullOrWhiteSpace(config.Password))
        {
            throw new InvalidOperationException("Email:Password no está configurado.");
        }
    }
}
