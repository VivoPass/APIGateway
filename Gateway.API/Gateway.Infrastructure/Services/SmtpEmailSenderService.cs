using Gateway.Infrastructure.DTOs;
using Gateway.Infrastructure.Exceptions;
using Gateway.Infrastructure.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Gateway.Infrastructure.Services
{
    public class SmtpEmailSenderService : ISmtpEmailSender
    {
        private readonly SmtpSettingsDto Settings;

        public SmtpEmailSenderService(IOptions<SmtpSettingsDto> options)
        {
            // 1. Inyección de Configuración y Logger
            Settings = options?.Value ?? throw new SmtpConfigNullException();

            // 2. Validación de Credenciales en el Constructor
            if (string.IsNullOrWhiteSpace(Settings.Correo) || string.IsNullOrWhiteSpace(Settings.Contrasena))
            {
                throw new SmtpConfigInvalidException();
            }
        }

        #region SendMailAsync(MailMessage mailMessage)
        /// <summary>
        /// Envía el mensaje de correo electrónico utilizando la configuración SMTP inyectada.
        /// </summary>
        public async Task SendCorreo(MailMessage mailMessage)
        {
            if (mailMessage == null)
            {
                throw new EmailMessageNullException();
            }

            // 1. Creación del cliente SMTP con la configuración inyectada
            using var smtpClient = new SmtpClient(Settings.Host, Settings.Port)
            {
                Credentials = new NetworkCredential(Settings.Correo, Settings.Contrasena),
                EnableSsl = Settings.EnableSsl
            };

            try
            {
                // 2. Envío Asíncrono
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (SmtpException smtpEx)
            {
                //Lanzamos una excepción más específica para la capa superior, envolviendo la original.
                throw new EmailSendSmtpException(smtpEx);
            }
            catch (Exception ex)
            {
                // Manejo de errores de red o DNS
                throw new EmailSendException(ex);
            }
        }
        #endregion

    }
}
