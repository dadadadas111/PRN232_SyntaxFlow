using System.Net.Mail;
using System.Threading.Tasks;

namespace Services
{
    public class EmailService : IEmailService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _pass;
        private readonly string _from;

        public EmailService(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _host = config["Smtp:Host"] ?? "smtp.gmail.com";
            _port = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
            _user = config["Smtp:User"] ?? "";
            _pass = config["Smtp:Pass"] ?? "";
            _from = _user;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            using var client = new SmtpClient(_host, _port)
            {
                Credentials = new System.Net.NetworkCredential(_user, _pass),
                EnableSsl = true
            };
            var mail = new MailMessage(_from, to, subject, body)
            {
                IsBodyHtml = true
            };
            await client.SendMailAsync(mail);
        }
    }
}
