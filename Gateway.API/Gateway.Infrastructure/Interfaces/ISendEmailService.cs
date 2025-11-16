
namespace Gateway.Infrastructure.Interfaces
{
    public interface ISendEmailService
    {
        //Task SendCorreoBase(string recipientEmail, string subject, string body);
        Task SendCorreoActualizacionContrasena(string recipientEmail, string subject, string body);
        Task SendCorreoActualizacionContrasenaConfirmacion(string email);
    }
}
