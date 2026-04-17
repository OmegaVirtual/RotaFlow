using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Rota.Helpers
{
    public static class EmailSender
    {
        private const string smtpServer = "smtp.mail.yahoo.com";
        private const int smtpPort = 587;

        private const string senderEmail = "cristina.cebuc93@yahoo.com"; // ✅ Your email
        private const string appPassword = "xkttgtmjpvmmsfmj"; // ✅ Not your real password

        public static async Task SendAsync(string toEmail, string subject, string body)
        {
            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(senderEmail, appPassword),
                EnableSsl = true
            };

            using var message = new MailMessage(senderEmail, toEmail, subject, body);
            await client.SendMailAsync(message);
        }
    }
}
