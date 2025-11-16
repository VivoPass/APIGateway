using Gateway.Infrastructure.Services;
using log4net;
using Moq;
using RestSharp;
using System.Net.Mail;
using Gateway.Infrastructure.Interfaces;

namespace Gateway.Tests.Gateway.Infraestructure.Services
{
    public class SendEmailServiceTests
    {
        private readonly Mock<ILog> _loggerMock;
        private readonly Mock<IRestClient> _restClientMock;
        private readonly Mock<ISmtpEmailSender> _smtpEmailSenderMock;
        private readonly sendEmailService _sendEmailService;

        public SendEmailServiceTests()
        {
            Environment.SetEnvironmentVariable("EMAIL", "no-reply@example.com"); // 🔹 Simular email válido

            _loggerMock = new Mock<ILog>();
            _restClientMock = new Mock<IRestClient>();
            _smtpEmailSenderMock = new Mock<ISmtpEmailSender>();

            _sendEmailService = new sendEmailService(_restClientMock.Object, _loggerMock.Object, _smtpEmailSenderMock.Object);
        }

        #region SendPasswordUpdateEmailAsync_Success()
        [Fact]
        public async Task SendPasswordUpdateEmailAsync_Success()
        {
            // Arrange
            var recipientEmail = "test@example.com";
            var subject = "Password Updated";
            var body = "Your password was updated successfully.";

            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sendEmailService.SendPasswordUpdateEmailAsync(recipientEmail, subject, body);

            // Assert
            Assert.Equal("Email sent successfully!", result);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s =>
                s.Contains($"Correo enviado exitosamente a {recipientEmail}"))), Times.Once);
        }
        #endregion

        #region SendPasswordUpdateEmailAsync_FailedToSend_ReturnsErrorMessage()
        [Fact]
        public async Task SendPasswordUpdateEmailAsync_FailedToSend_ReturnsErrorMessage()
        {
            // Arrange
            var recipientEmail = "test@example.com";
            var subject = "Password Updated";
            var body = "Your password was updated successfully.";

            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .ThrowsAsync(new SmtpException("SMTP Error"));

            // Act
            var result = await _sendEmailService.SendPasswordUpdateEmailAsync(recipientEmail, subject, body);

            // Assert
            Assert.Contains("Error sending email: SMTP Error", result);
            _loggerMock.Verify(x =>
                x.Error(It.Is<string>(s => s.Contains($"Error al enviar correo a {recipientEmail}")),
                    It.IsAny<Exception>()), Times.Once);
        }
        #endregion

        #region SendPasswordUpdateEmailAsync_UnexpectedError_ReturnsInternalErrorMessage()
        [Fact]
        public async Task SendPasswordUpdateEmailAsync_UnexpectedError_ReturnsInternalErrorMessage()
        {
            // Arrange
            var recipientEmail = "test@example.com";
            var subject = "Password Updated";
            var body = "Your password was updated successfully.";

            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .ThrowsAsync(new Exception("Unexpected Error"));

            // Act
            var result = await _sendEmailService.SendPasswordUpdateEmailAsync(recipientEmail, subject, body);

            // Assert
            Assert.Equal("Error interno al enviar correo.", result);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s =>
                    s.Contains($"Error inesperado en la preparación del correo para {recipientEmail}")),
                It.IsAny<Exception>()), Times.Once);
        }
        #endregion

        #region SendPasswordUpdateEmailAsync_MissingSenderEmail_ThrowsArgumentNullException()
        /*[Fact]
        public void SendPasswordUpdateEmailAsync_MissingSenderEmail_ThrowsArgumentNullException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("EMAIL", null); // 🔹 Simular que el email del remitente no está configurado

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new sendEmailService(_restClientMock.Object, _loggerMock.Object, _smtpEmailSenderMock.Object)
            );

            Assert.Equal("_emailFrom", exception.ParamName);
        }*/
        #endregion


        #region SendPasswordUpdateEmail_Success()
        [Fact]
        public async Task SendPasswordUpdateEmail_Success()
        {
            // Arrange
            var email = "test@example.com";

            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .Returns(Task.CompletedTask);

            // Act
            await _sendEmailService.SendPasswordUpdateEmail(email);

            // Assert
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains($"Correo de confirmación de contraseña enviado a {email}"))), Times.Once);
        }
        #endregion

        #region SendPasswordUpdateEmail_FailedToSend_ThrowsSmtpException()
        [Fact]
        public async Task SendPasswordUpdateEmail_FailedToSend_LogsWarning()
        {
            // Arrange
            var email = "test@example.com";

            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .ThrowsAsync(new SmtpException("SMTP Error"));

            // Act
            await _sendEmailService.SendPasswordUpdateEmail(email);

            // Assert
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al enviar correo de confirmación de cambio de contraseña para {email}"))), Times.Once);
        }
        #endregion

        #region SendPasswordUpdateEmail_UnexpectedError_LogsError()
        [Fact]
        public async Task SendPasswordUpdateEmail_UnexpectedError_LogsError()
        {
            // Arrange
            var email = "test@example.com";

            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .ThrowsAsync(new Exception("Unexpected Error"));

            // Act
            await _sendEmailService.SendPasswordUpdateEmail(email);

            // Assert
            _loggerMock.Verify(x => x.Error(It.Is<string>(s =>
                    s.Contains($"Error inesperado en la preparación del correo para {email}")),
                It.IsAny<Exception>()), Times.Once);
        }
        #endregion

    }
}
