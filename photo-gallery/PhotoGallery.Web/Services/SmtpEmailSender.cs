// This page ends HTML emails using MailKit + config from appsettings. Handy for registration confirmations.
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace PhotoGallery.Web.Services
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _cfg;
        private readonly ILogger<SmtpEmailSender> _log;

        public SmtpEmailSender(IConfiguration cfg, ILogger<SmtpEmailSender> log)
        {
            _cfg = cfg;
            _log = log;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var host     = _cfg["Email:Smtp:Host"];
            var portStr  = _cfg["Email:Smtp:Port"];
            var user     = _cfg["Email:Smtp:User"];
            var pass     = _cfg["Email:Smtp:Pass"];
            var secure   = _cfg["Email:Smtp:Secure"] ?? "StartTls"; 
            var fromAddr = _cfg["Email:From:Address"];
            var fromName = _cfg["Email:From:Name"] ?? "Photo Gallery";

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddr))
            {
                _log.LogError("Email sender is not configured. Missing Host or From address.");
                return;
            }

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(fromName, fromAddr));
            msg.To.Add(new MailboxAddress(email, email));
            msg.Subject = subject;

            var body = new BodyBuilder { HtmlBody = htmlMessage };
            msg.Body = body.ToMessageBody();

            var port = int.TryParse(portStr, out var p) ? p : 587;
            var secureOption = secure.Equals("SslOnConnect", System.StringComparison.OrdinalIgnoreCase)
                ? SecureSocketOptions.SslOnConnect
                : secure.Equals("None", System.StringComparison.OrdinalIgnoreCase)
                    ? SecureSocketOptions.None
                    : SecureSocketOptions.StartTls;

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(host, port, secureOption);
                if (!string.IsNullOrEmpty(user))
                    await client.AuthenticateAsync(user, pass);

                await client.SendAsync(msg);
                _log.LogInformation("Email to {Email} sent: {Subject}", email, subject);
            }
            catch (System.Exception ex)
            {
                _log.LogError(ex, "Failed sending email to {Email}", email);
            }
            finally
            {
                try { await client.DisconnectAsync(true); } catch {}
            }
        }
    }
}
