using Gateway.Infrastructure.Interfaces;
using Gateway.API.Controllers;
using Gateway.Infrastructure.DTOs;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Gateway.Tests.Gateway.API.Gateway.Controllers
{
    public class RegisterTests
    {
        private readonly Mock<ILog> _loggerMock;
        private readonly Mock<IKeycloakService> _keycloakServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly GatewayController _gatewayController;

        public RegisterTests()
        {
            _loggerMock = new Mock<ILog>();
            _keycloakServiceMock = new Mock<IKeycloakService>();
            _userServiceMock = new Mock<IUserService>();
            _gatewayController = new GatewayController(null, null, _loggerMock.Object, _keycloakServiceMock.Object, _userServiceMock.Object);
        }

        [Fact]
        public async Task Register_Success_ReturnsOk()
        {
            // Arrange
            var request = new createUserDto { Email = "test@example.com", Name = "Test", LastName = "User", Password = "securepassword", RoleId = "admin-id" };
            var token = "valid-token";

            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.RegisterUserInKeycloak(request, token)).ReturnsAsync(true);
            _userServiceMock.Setup(u => u.RegisterUserInDatabase(request, token)).ReturnsAsync(true);
            _keycloakServiceMock.Setup(k => k.SendUserVerifyMail(request.Email, token)).ReturnsAsync(1);

            // Act
            var result = await _gatewayController.Register(request) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Usuario registrado exitosamente.", result.Value);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains($"Intento de registro para usuario: {request.Email}"))), Times.Once);
        }

        [Fact]
        public async Task Register_KeycloakError_ReturnsBadRequest()
        {
            // Arrange
            var request = new createUserDto { Email = "test@example.com", Name = "Test", LastName = "User", Password = "securepassword", RoleId = "admin-id" };
            var token = "valid-token";

            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.RegisterUserInKeycloak(request, token)).ReturnsAsync(false);

            // Act
            var result = await _gatewayController.Register(request) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Error al registrar usuario en Keycloak.", result.Value);
        }

        [Fact]
        public async Task Register_DatabaseError_ReturnsBadRequest()
        {
            // Arrange
            var request = new createUserDto { Email = "test@example.com", Name = "Test", LastName = "User", Password = "securepassword", RoleId = "admin-id" };
            var token = "valid-token";

            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.RegisterUserInKeycloak(request, token)).ReturnsAsync(true);
            _userServiceMock.Setup(u => u.RegisterUserInDatabase(request, token)).ReturnsAsync(false);

            // Act
            var result = await _gatewayController.Register(request) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Error al registrar usuario en la base de datos.", result.Value);
        }

        [Fact]
        public async Task Register_VerificationEmailError_ReturnsBadRequest()
        {
            // Arrange
            var request = new createUserDto { Email = "test@example.com", Name = "Test", LastName = "User", Password = "securepassword", RoleId = "admin-id" };
            var token = "valid-token";

            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ReturnsAsync(token);
            _keycloakServiceMock.Setup(k => k.RegisterUserInKeycloak(request, token)).ReturnsAsync(true);
            _userServiceMock.Setup(u => u.RegisterUserInDatabase(request, token)).ReturnsAsync(true);
            _keycloakServiceMock.Setup(k => k.SendUserVerifyMail(request.Email, token)).ReturnsAsync(-1);

            // Act
            var result = await _gatewayController.Register(request) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Error al enviar correo de verificación.", result.Value);
        }

        /*[Fact]
        public async Task Register_UnexpectedError_ReturnsInternalServerError()
        {
            // Arrange
            var request = new createUserDto { Email = "test@example.com", Name = "Test", LastName = "User", Password = "securepassword", RoleId = "admin-id" };

            _keycloakServiceMock.Setup(k => k.GetTokenAsync()).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _gatewayController.Register(request) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal("Error interno en el servidor.", result.Value);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains($"Error inesperado en el proceso de registro para {request.Email}"))), Times.Once);
        }*/
    }
}
