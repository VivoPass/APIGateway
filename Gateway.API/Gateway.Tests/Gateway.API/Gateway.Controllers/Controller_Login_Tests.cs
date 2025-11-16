using Gateway.API.Controllers;
using Gateway.API.DTOs;
using Gateway.API.DTOs;
using Gateway.Infrastructure.Interfaces;
using Gateway.Infrastructure.Services;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RestSharp;
using System.Reflection;
using System.Text.Json;


namespace Gateway.Tests.Gateway.API.Gateway.Controllers
{
    public class LoginTests
    {
        private readonly Mock<ILog> _loggerMock;
        private readonly Mock<IKeycloakService> _keycloakServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly GatewayController _gatewayController;

        public LoginTests()
        {
            _loggerMock = new Mock<ILog>();
            _keycloakServiceMock = new Mock<IKeycloakService>();
            _userServiceMock = new Mock<IUserService>();
            _gatewayController = new GatewayController(null, null, _loggerMock.Object, _keycloakServiceMock.Object, _userServiceMock.Object);
        }

        /*[Fact]
        public async Task Login_Success_ReturnsOk()
        {
            // Arrange
            var request = new loginDto { Email = "test@example.com", Password = "securepassword" };
            var accessToken = "valid-access-token";
            var refreshToken = "valid-refresh-token";
            var expiresIn = 3600;
            var userId = "12345";
            var userRoleId = "admin";

            _keycloakServiceMock.Setup(k => k.AuthenticateWithKeycloak(request.Email, request.Password))
                .ReturnsAsync((accessToken, refreshToken, expiresIn));

            _userServiceMock.Setup(u => u.GetUserFromService(request.Email, accessToken))
                .ReturnsAsync((userId, userRoleId));

            _userServiceMock.Setup(u => u.saveUserActivity(userId, "LOGIN", accessToken))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _gatewayController.Login(request) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains($"Intento de inicio de sesión para usuario: {request.Email}"))), Times.Once);
            Assert.Equal(accessToken, ((dynamic)result.Value).access_token);
        }*/

        #region Login_InvalidCredentials_ReturnsBadRequest()
        [Fact]
        public async Task Login_InvalidCredentials_ReturnsBadRequest()
        {
            // Arrange
            var request = new loginDto { Email = "test@example.com", Password = "wrongpassword" };

            _keycloakServiceMock.Setup(k => k.AuthenticateWithKeycloak(request.Email, request.Password))
                .ReturnsAsync((null, null, 0));

            // Act
            var result = await _gatewayController.Login(request) as BadRequestObjectResult;

            // Assert
            //Assert.NotNull(result);
            //Assert.Equal(400, result.StatusCode);
            Assert.Equal("Credenciales inválidas. Error de autenticación en Keycloak.", result.Value);
        }
        #endregion

        #region Login_UserNotFound_ReturnsBadRequest()
        [Fact]
        public async Task Login_UserNotFound_ReturnsBadRequest()
        {
            // Arrange
            var request = new loginDto { Email = "test@example.com", Password = "securepassword" };
            var accessToken = "valid-access-token";
            var refreshToken = "valid-refresh-token";
            var expiresIn = 3600;

            _keycloakServiceMock.Setup(k => k.AuthenticateWithKeycloak(request.Email, request.Password))
                .ReturnsAsync((accessToken, refreshToken, expiresIn));

            _userServiceMock.Setup(u => u.GetUserFromService(request.Email, accessToken))
                .ReturnsAsync((null, null));

            // Act
            var result = await _gatewayController.Login(request) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Error al obtener usuario en la base de datos.", result.Value);
        }
        #endregion

        /*[Fact]
        public async Task Login_UnexpectedError_ReturnsInternalServerError()
        {
            // Arrange
            var request = new loginDto { Email = "test@example.com", Password = "securepassword" };

            _keycloakServiceMock.Setup(k => k.AuthenticateWithKeycloak(request.Email, request.Password))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _gatewayController.Login(request) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal("Error interno en el servidor.", result.Value);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains($"Error inesperado en el proceso de login para {request.Email}"))), Times.Once);
        }*/
    }
}
