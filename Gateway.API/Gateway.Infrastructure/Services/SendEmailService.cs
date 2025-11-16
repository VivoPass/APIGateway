using Gateway.Infrastructure.Exceptions;
using Gateway.Infrastructure.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gateway.Infrastructure.Services
{
    public class SendEmailService : ISendEmailService
    {
        private readonly ISmtpEmailSender SmtpEmailSender;
        private readonly string EmailFrom;

        public SendEmailService(ISmtpEmailSender smtpEmailSender)
        {
            SmtpEmailSender = smtpEmailSender ?? throw new ArgumentNullException(nameof(smtpEmailSender));

            // La validación en el constructor es correcta.
            EmailFrom = Environment.GetEnvironmentVariable("EMAIL");
            if (string.IsNullOrWhiteSpace(EmailFrom))
            {
                throw new EmailNullException();
            }
        }

        #region SendCorreoBase(string recipientEmail, string subject, string body)
        /// <summary>
        /// Construye y envía un correo electrónico. Este método centraliza la lógica de SMTP y manejo de excepciones.
        /// </summary>
        private async Task SendCorreoBase(string recipientEmail, string subject, string body)
        {
            // 1. Validación
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                throw new RecipientEmailNullException();
            }

            try
            {
                // 2. Creación del Mensaje
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(EmailFrom),
                    Subject = subject,
                    Body = body,
                    // Asumimos IsBodyHtml = true para la mayoría de correos modernos, cámbialo si es solo texto.
                    IsBodyHtml = true
                };

                mailMessage.To.Add(recipientEmail);

                // 3. Envío
                await SmtpEmailSender.SendCorreo(mailMessage);
            }
            catch (SmtpException smtpEx)
            {
                // Encapsulamos el error de SMTP en una excepción de la aplicación.
                throw new CorreoSendSmtpException(smtpEx);
            }
            catch (Exception ex)
            {
                // Capturamos otros errores de preparación o configuración.
                throw new CorreoSendException(ex);
            }
        }
        #endregion

        #region SendCorreoActualizacionContrasena(string recipientEmail, string subject, string body)
        /// <summary>
        /// Envía un correo con un asunto y cuerpo específicos (el antiguo SendPasswordUpdateEmailAsync).
        /// Se cambia el retorno de string a Task.
        /// </summary>
        public Task SendCorreoActualizacionContrasena(string recipientEmail, string subject, string body)
            {
                // Delegamos todo el trabajo y el manejo de errores al método base
                return SendCorreoBase(recipientEmail, subject, body);
            }
        #endregion

        #region SendCorreoActualizacionContrasenaConfirmacion(string email)
        /// <summary>
        /// Envía el correo de confirmación de cambio de contraseña con texto fijo (el antiguo SendPasswordUpdateEmail).
        /// Se cambia el retorno de Task a Task y se remueve el manejo de string.
        /// </summary>
        public Task SendCorreoActualizacionContrasenaConfirmacion(string email)
        {
            const string subject = "Password Updated Successfully!";
            const string body = "Congratulations! Your password has been updated successfully.";

            return SendCorreoBase(email, subject, body);
        }
        #endregion
    }
}
