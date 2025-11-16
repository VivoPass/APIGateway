using System.Net.Mail;

namespace Gateway.Infrastructure.Interfaces
{
    public interface ISmtpEmailSender
    {
        Task SendCorreo(MailMessage mailMessage);
    }
}
