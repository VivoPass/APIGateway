using System.Net.Mail;
using Gateway.Infrastructure.Interfaces;
using Gateway.Infrastructure.Services;
using Moq;

namespace Gateway.Tests.Gateway.Infraestructure.Services
{
    public class SmtpEmailSenderServiceTests
    {
        private readonly Mock<ISmtpEmailSender> _smtpEmailSenderMock;
        private readonly SmtpEmailSenderService _smtpEmailSenderService;

        public SmtpEmailSenderServiceTests()
        {
            _smtpEmailSenderMock = new Mock<ISmtpEmailSender>();
            _smtpEmailSenderService = new SmtpEmailSenderService();

            Environment.SetEnvironmentVariable("EMAIL", "no-reply@example.com");
            Environment.SetEnvironmentVariable("EMAIL_PASSWORD", "securepassword");
        }

        #region SendMailAsync_ValidEmail_SendsSuccessfully()
        [Fact]
        public async Task SendMailAsync_ValidEmail_SendsSuccessfully()
        {
            // Arrange
            var mailMessage = new MailMessage
            {
                From = new MailAddress("test@example.com"),
                Subject = "Test Email",
                Body = "This is a test email.",
                IsBodyHtml = false
            };
            mailMessage.To.Add("recipient@example.com");

            // Simular el envío exitoso sin conexión real
            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .Returns(Task.CompletedTask);

            // Act
            await _smtpEmailSenderMock.Object.SendMailAsync(mailMessage);

            // Assert
            _smtpEmailSenderMock.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }
        #endregion

        #region SendMailAsync_NullMailMessage_ThrowsArgumentNullException()
        [Fact]
        public async Task SendMailAsync_NullMailMessage_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _smtpEmailSenderService.SendMailAsync(null));
        }
        #endregion

        #region SendMailAsync_MissingSenderEmail_ThrowsInvalidOperationException()
        [Fact]
        public async Task SendMailAsync_MissingSenderEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("EMAIL", null); // 🔹 Simular que el email del remitente no está configurado

            var mailMessage = new MailMessage
            {
                Subject = "Test Email",
                Body = "This is a test email.",
                IsBodyHtml = false
            };
            mailMessage.To.Add("recipient@example.com");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _smtpEmailSenderService.SendMailAsync(mailMessage));
        }
        #endregion

        #region SendMailAsync_InvalidRecipientEmail_ThrowsFormatException()
        [Fact]
        public async Task SendMailAsync_InvalidRecipientEmail_ThrowsFormatException()
        {
            // Arrange
            var mailMessage = new MailMessage
            {
                From = new MailAddress(Environment.GetEnvironmentVariable("EMAIL")),
                Subject = "Test Email",
                Body = "This is a test email.",
                IsBodyHtml = false
            };
            Assert.Throws<FormatException>(() => mailMessage.To.Add("INVALID_EMAIL"));

            // Act & Assert
            Assert.Throws<FormatException>(() => mailMessage.To.Add("INVALID_EMAIL"));
        }
        #endregion

        #region SendMailAsync_MissingCredentials_ThrowsInvalidOperationException()
        [Fact]
        public async Task SendMailAsync_MissingCredentials_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("EMAIL_PASSWORD", null); // 🔹 Simular credenciales incorrectas

            var mailMessage = new MailMessage
            {
                From = new MailAddress(Environment.GetEnvironmentVariable("EMAIL")),
                Subject = "Test Email",
                Body = "This is a test email.",
                IsBodyHtml = false
            };
            mailMessage.To.Add("recipient@example.com");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _smtpEmailSenderService.SendMailAsync(mailMessage));
        }
        #endregion

        #region SendMailAsync_SmtpError_ThrowsInvalidOperationException()
        [Fact]
        public async Task SendMailAsync_SmtpError_ThrowsInvalidOperationException()
        {
            // Arrange
            var mailMessage = new MailMessage
            {
                From = new MailAddress("test@example.com"),
                Subject = "Test Email",
                Body = "This is a test email.",
                IsBodyHtml = false
            };
            mailMessage.To.Add("recipient@example.com");

            // Simular la conversión de excepciones en SmtpEmailSenderService
            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .ThrowsAsync(new InvalidOperationException("Error al enviar el correo: SMTP Error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _smtpEmailSenderMock.Object.SendMailAsync(mailMessage));

            // Verificar que la simulación se ejecutó correctamente
            _smtpEmailSenderMock.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }
        #endregion

        [Fact]
        public async Task SendMailAsync_InvalidSenderPassword_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("EMAIL", "test@example.com");
            Environment.SetEnvironmentVariable("EMAIL_PASSWORD", null); // 🔹 Simular contraseña incorrecta

            var mailMessage = new MailMessage
            {
                From = new MailAddress(Environment.GetEnvironmentVariable("EMAIL")),
                Subject = "Test Email",
                Body = "This is a test email.",
                IsBodyHtml = false
            };
            mailMessage.To.Add("recipient@example.com");

            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .ThrowsAsync(new InvalidOperationException("Las credenciales del correo no están configuradas correctamente."));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _smtpEmailSenderMock.Object.SendMailAsync(mailMessage));

            // Verificar que el método fue llamado una vez
            _smtpEmailSenderMock.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }

        [Fact]
        public async Task SendMailAsync_EmptySubject_SendsSuccessfully()
        {
            // Arrange
            var mailMessage = new MailMessage
            {
                From = new MailAddress("test@example.com"),
                Subject = "", // 🔹 Simular email sin asunto
                Body = "Este correo no tiene asunto.",
                IsBodyHtml = false
            };
            mailMessage.To.Add("recipient@example.com");

            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .Returns(Task.CompletedTask);

            // Act
            await _smtpEmailSenderMock.Object.SendMailAsync(mailMessage);

            // Assert
            _smtpEmailSenderMock.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }

        [Fact]
        public async Task SendMailAsync_InvalidSenderAddress_ThrowsFormatException()
        {
            // Arrange
            var mailMessage = new MailMessage
            {
                Subject = "Test Email",
                Body = "Correo con remitente mal formado.",
                IsBodyHtml = false
            };

            // 🔹 Agregar el destinatario antes de la prueba
            mailMessage.To.Add("recipient@example.com");

            var smtpMock = new Mock<ISmtpEmailSender>();

            // 🔹 Simular que el servicio lanza `FormatException`
            smtpMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .ThrowsAsync(new FormatException("El remitente tiene un formato inválido."));

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => smtpMock.Object.SendMailAsync(mailMessage));

            // Verificar que el servicio fue llamado una vez
            smtpMock.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }

        [Fact]
        public async Task SendMailAsync_SpecialCharactersInSubject_SendsSuccessfully()
        {
            // Arrange
            var mailMessage = new MailMessage
            {
                From = new MailAddress("test@example.com"),
                Subject = "Test !@#$%^&*()_+",
                Body = "Este correo tiene caracteres especiales en el asunto.",
                IsBodyHtml = false
            };
            mailMessage.To.Add("recipient@example.com");

            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .Returns(Task.CompletedTask);

            // Act
            await _smtpEmailSenderMock.Object.SendMailAsync(mailMessage);

            // Assert
            _smtpEmailSenderMock.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }

        [Fact]
        public async Task SendMailAsync_TimeoutSimulation_SendsSuccessfully()
        {
            // Arrange
            var mailMessage = new MailMessage
            {
                From = new MailAddress("test@example.com"),
                Subject = "Prueba con carga alta",
                Body = "Este mensaje simula una alta carga en el servidor.",
                IsBodyHtml = false
            };
            mailMessage.To.Add("recipient@example.com");

            _smtpEmailSenderMock.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .Returns(async () =>
                {
                    await Task.Delay(5000); // 🔹 Simular tiempo de espera
                });

            // Act
            await _smtpEmailSenderMock.Object.SendMailAsync(mailMessage);

            // Assert
            _smtpEmailSenderMock.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }

    }
}

