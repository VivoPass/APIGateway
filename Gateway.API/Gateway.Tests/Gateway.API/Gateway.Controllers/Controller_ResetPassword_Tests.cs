using Gateway.API.Controllers;
using Gateway.Infrastructure.Interfaces;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RestSharp;
using System.Net;

namespace Gateway.Tests.Gateway.API.Gateway.Controllers
{
    public class ResetPasswordTests
    {
        private readonly Mock<ILog> _loggerMock;
        private readonly Mock<IKeycloakService> _keycloakServiceMock;
        private readonly Mock<IRestClient> _restClientMock;
        private readonly GatewayController _gatewayController;

        public ResetPasswordTests()
        {
            _loggerMock = new Mock<ILog>();
            _keycloakServiceMock = new Mock<IKeycloakService>();
            _restClientMock = new Mock<IRestClient>();
            _gatewayController = new GatewayController(_restClientMock.Object, null, _loggerMock.Object, _keycloakServiceMock.Object, null);
        }

        [Fact]
        public async Task ResetPassword_Success_ReturnsOk()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";
            var userId = "12345";

            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token)).ReturnsAsync(userId);

            var restResponse = new RestResponse
            {
                StatusCode = System.Net.HttpStatusCode.NoContent,
                ResponseStatus = ResponseStatus.Completed,
                IsSuccessStatusCode = true
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _gatewayController.ResetPassword(email) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Correo Enviado!", result.Value);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains($"Correo de recuperación enviado correctamente a {email}"))), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_UserNotFound_ReturnsBadRequest()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";

            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token)).ReturnsAsync(string.Empty); // 🔹 Simular usuario no encontrado

            // Act
            var result = await _gatewayController.ResetPassword(email) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("User not Found", result.Value);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Usuario no encontrado en Keycloak: {email}"))), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_EmailRequestError_ReturnsBadRequest()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";
            var userId = "12345";

            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token)).ReturnsAsync(userId);

            var restResponse = new RestResponse
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest, // 🔹 Simular error en el envío de email
                ErrorMessage = "Bad Request"
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _gatewayController.ResetPassword(email) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("Error al enviar correo", result.Value.ToString());
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al enviar correo de recuperación de contraseña"))), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_EmailRequestUnexpectedError_ReturnsInternalServerError()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";
            var userId = "12345";

            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token)).ReturnsAsync(userId);

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _gatewayController.ResetPassword(email) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal("Error interno al enviar el correo.", result.Value);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => 
                s.Contains("Error en la solicitud de correo de recuperación para test@example.com: Unexpected error"))), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_UnexpectedError_ReturnsInternalServerError()
        {
            // Arrange
            var email = "test@example.com";

            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _gatewayController.ResetPassword(email) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal("Error interno en el servidor.", result.Value);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => 
                s.Contains($"Error inesperado en el proceso de restablecimiento de contraseña para {email}"))), Times.Once);
        }
    }
}
