using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gateway.Infrastructure.Exceptions
{
    /*/////////////////INFRAESTRUCTURA/////////////////*/
    #region KEYCLOAK SERVICE EXCEPTIONS
    public class KeycloakTokenException : Exception
    {
        public KeycloakTokenException() : base("No se pudo obtener el token de servicio de Keycloak.")
        { }
    }

    public class KeycloakCommunicationException : Exception
    {
        public KeycloakCommunicationException(Exception inner) : base("Error de comunicación o parsing del token.", inner)
        { }
    }

    public class KeycloakCommunicationMailException : Exception
    {
        public KeycloakCommunicationMailException() : base("Error Keycloak al enviar el correo.")
        { }
    }

    public class KeycloakAccessDeniedException : Exception
    {
        public KeycloakAccessDeniedException() : base("Acceso denegado en Keycloak.")
        { }
    }
    #endregion

    #region USER SERVICE EXCEPTIONS
    public class UserServiceException : Exception
    {
        public UserServiceException() : base("Error del Microservicio.")
        { }
    }
    #endregion

    #region SEND EMAIL SERVICE EXCEPTIONS
    public class EmailNullException : Exception
    {
        public EmailNullException() : base("El email del remitente no está configurado.")
        { }
    }
    public class RecipientEmailNullException : Exception
    {
        public RecipientEmailNullException() : base("El email del destinatario no puede ser nulo o vacío.")
        { }
    }
    public class CorreoSendSmtpException : Exception
    {
        public CorreoSendSmtpException(Exception inner) : base("Error de SMTP al enviar correo.", inner)
        { }
    }
    public class CorreoSendException : Exception
    {
        public CorreoSendException(Exception inner) : base("Error inesperado al enviar el correo.", inner)
        { }
    }
    #endregion

    #region SMPT EMAIL SENDER SERVICE EXCEPTIONS
    public class EmailSendSmtpException : Exception
    {
        public EmailSendSmtpException(Exception inner) : base("Error al enviar el correo debido a una configuración o fallo del servidor SMTP.", inner)
        { }
    }
    public class EmailSendException : Exception
    {
        public EmailSendException(Exception inner) : base("Error inesperado en la infraestructura de red al enviar el correo.", inner)
        { }
    }
    public class EmailMessageNullException : Exception
    {
        public EmailMessageNullException() : base("El mensaje de correo no puede ser nulo.")
        { }
    }
    public class SmtpConfigNullException : Exception
    {
        public SmtpConfigNullException() : base("La configuración de SMTP es nula.")
        { }
    }
    public class SmtpConfigInvalidException : Exception
    {
        public SmtpConfigInvalidException() : base("Las credenciales de SMTP no están configuradas correctamente. Verifique la sección 'SmtpSettingsDto'.")
        { }
    }
    #endregion

    #region RESTCLIENT EXCEPTIONS
    //SendEndpointProvider Null Exception
    public class RestClientNullException : Exception
    {
        public RestClientNullException() : base("No se pudo acceder al cliente HTTP (IRestClient).")
        { }
    }
    #endregion

}
