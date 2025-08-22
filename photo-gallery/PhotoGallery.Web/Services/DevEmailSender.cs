// - Implements IEmailSender but writes the email to an .html file on disk. It Lets you open the confirmation email and click the link without SMTP creds

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;

namespace PhotoGallery.Web.Services
{
    public class DevEmailSender : IEmailSender
    {
        private readonly string _dropDir;

        public DevEmailSender(IWebHostEnvironment env, IConfiguration cfg)
        {
            _dropDir = cfg["Email:PickupDirectory"]
                       ?? Path.Combine(env.ContentRootPath, "EmailDrop");
            Directory.CreateDirectory(_dropDir);
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var safe = new string((email ?? "unknown").Where(char.IsLetterOrDigit).ToArray());
            var name = $"{DateTime.UtcNow:yyyyMMdd_HHmmssfff}_{safe}.html";
            var path = Path.Combine(_dropDir, name);

            var payload = $"<h3>To: {email}</h3><h4>Subject: {subject}</h4><hr/>{htmlMessage}";
            return File.WriteAllTextAsync(path, payload);
        }
    }
}
