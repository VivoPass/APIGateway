using System.Security.Claims;
using Gateway.API.Controllers;
using Gateway.API.DTOs;
using Gateway.API.DTOs;
using Gateway.Infrastructure.Interfaces;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Gateway.Tests.Gateway.API.Gateway.Controllers
{

    public class UpdatePasswordTests
    {
        private readonly Mock<ILog> _loggerMock;
        private readonly Mock<IKeycloakService> _keycloakServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ISendEmailService> _sendEmailServiceMock;
        private readonly GatewayController _gatewayController;

        public UpdatePasswordTests()
        {
            _loggerMock = new Mock<ILog>();
            _keycloakServiceMock = new Mock<IKeycloakService>();
            _userServiceMock = new Mock<IUserService>();
            _sendEmailServiceMock = new Mock<ISendEmailService>();
            _gatewayController = new GatewayController(null, _sendEmailServiceMock.Object, _loggerMock.Object, _keycloakServiceMock.Object, _userServiceMock.Object);
        }

        [Fact]
        public async Task UpdatePassword_Success_ReturnsOk()
        {
            // Arrange
            var request = new updatePasswordDto { Password = "newSecurePassword" };
            var email = "test@example.com";
            var token = "valid-token";
            var userId = "12345";

            //Mockear el contexto de usuario con Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _gatewayController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            //Mockear servicios
            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token)).ReturnsAsync(userId);
            _keycloakServiceMock.Setup(k => k.UpdateUserPasswordInKeycloak(userId, request.Password, token)).ReturnsAsync(true);
            _userServiceMock.Setup(u => u.GetUserIdFromDatabase(email, token)).ReturnsAsync(userId);
            _userServiceMock.Setup(u => u.saveUserActivity(userId, "PASSWORD_CHANGED", token)).Returns(Task.CompletedTask);
            _sendEmailServiceMock.Setup(s => s.SendPasswordUpdateEmail(email)).Returns(Task.CompletedTask);

            // Act
            var result = await _gatewayController.UpdatePassword(request) as OkObjectResult;

            // Assert
            _loggerMock.Verify(x => x.Info(It.Is<string>(s =>
                s.Contains($"Actividad de cambio de contraseña registrada para usuario {email}"))), Times.Once);

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Contraseña actualizada correctamente.", result.Value);
        }

        [Fact]
        public async Task UpdatePassword_UserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            var request = new updatePasswordDto { Password = "newSecurePassword" };

            // 🔹 Simular que no hay usuario autenticado
            _gatewayController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // 🔹 Sin usuario asignado en el contexto
            };

            // Act
            var result = await _gatewayController.UpdatePassword(request) as UnauthorizedObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
            Assert.Equal("No se pudo obtener el email del usuario.", result.Value);
        }

        [Fact]
        public async Task UpdatePassword_UserNotFound_ReturnsBadRequest()
        {
            // Arrange
            var request = new updatePasswordDto { Password = "newSecurePassword" };
            var email = "test@example.com";
            var token = "valid-token";

            // 🔹 Simular que el usuario tiene un email en los Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _gatewayController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // 🔹 Mockear servicios
            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token)).ReturnsAsync(string.Empty); // Simular que el usuario no existe en Keycloak

            // Act
            var result = await _gatewayController.UpdatePassword(request) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("User not found.", result.Value);
        }

        [Fact]
        public async Task UpdatePassword_UpdateError_ReturnsBadRequest()
        {
            // Arrange
            var request = new updatePasswordDto { Password = "newSecurePassword" };
            var email = "test@example.com";
            var token = "valid-token";
            var userId = "12345";

            // 🔹 Simular que el usuario tiene un email en los Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _gatewayController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // 🔹 Mockear servicios
            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token)).ReturnsAsync(userId);
            _keycloakServiceMock.Setup(k => k.UpdateUserPasswordInKeycloak(userId, request.Password, token)).ReturnsAsync(false); // Simular fallo al actualizar la contraseña

            // Act
            var result = await _gatewayController.UpdatePassword(request) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Error al actualizar la contraseña en Keycloak.", result.Value);
        }

        [Fact]
        public async Task UpdatePassword_UserIdDatabaseError_ReturnsInternalServerError()
        {
            // Arrange
            var request = new updatePasswordDto { Password = "newSecurePassword" };
            var email = "test@example.com";
            var token = "valid-token";
            var userId = "12345";

            // 🔹 Simular que el usuario tiene un email en los Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _gatewayController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // 🔹 Mockear servicios
            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token)).ReturnsAsync(userId);
            _keycloakServiceMock.Setup(k => k.UpdateUserPasswordInKeycloak(userId, request.Password, token)).ReturnsAsync(true);
            _userServiceMock.Setup(u => u.GetUserIdFromDatabase(email, token)).ReturnsAsync(string.Empty); // 🔹 Simular error al obtener el usuario de la base de datos

            // Act
            var result = await _gatewayController.UpdatePassword(request) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal("Error interno al obtener el usuario.", result.Value);
        }

        [Fact]
        public async Task UpdatePassword_UnexpectedError_ReturnsInternalServerError()
        {
            // Arrange
            var request = new updatePasswordDto { Password = "newSecurePassword" };
            var email = "test@example.com";

            //Simular que el usuario tiene un email en los Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _gatewayController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            //Mockear Keycloak para lanzar una excepción inesperada
            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _gatewayController.UpdatePassword(request) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal("Error interno en el servidor.", result.Value);
        }

    }
}
