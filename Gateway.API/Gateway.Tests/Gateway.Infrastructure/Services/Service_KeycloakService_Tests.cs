using Gateway.Infrastructure.DTOs;
using Gateway.Infrastructure.Interfaces;
using Gateway.Infrastructure.Services;
using log4net;
using Moq;
using RestSharp;
using System.Net;
using System.Text.Json;


namespace Gateway.Tests.Gateway.Infraestructure.Services
{
    public class KeycloakServiceTests
    {
        private readonly Mock<IRestClient> _restClientMock;
        private readonly Mock<ILog> _loggerMock;
        private readonly KeycloakService _keycloakService;
        private readonly Mock<IKeycloakService> _keycloakServiceMock;

        public KeycloakServiceTests()
        {
            _restClientMock = new Mock<IRestClient>();
            _loggerMock = new Mock<ILog>();
            _keycloakServiceMock = new Mock<IKeycloakService>();
            _keycloakService = new KeycloakService(_restClientMock.Object, _loggerMock.Object);
        }

        #region GetKeycloakUserIdByEmail_Success_ReturnsUserId()
        [Fact]
        public async Task GetKeycloakUserIdByEmail_Success_ReturnsUserId()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";
            var userId = "12345";
            var jsonResponse = $"[{{\"id\": \"{userId}\"}}]"; // 🔹 Simular respuesta correcta

            var restResponse = new RestResponse
            {
                Content = jsonResponse,
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK,
                IsSuccessStatusCode = true
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakService.GetKeycloackUserIdByEmail(email, token);

            // Assert
            Assert.Equal(userId, result);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains($"Usuario encontrado en Keycloak: {userId}"))), Times.Once);
        }
        #endregion

        #region GetKeycloakUserIdByEmail_UserNotFound_ReturnsEmptyString()
        [Fact]
        public async Task GetKeycloakUserIdByEmail_UserNotFound_ReturnsEmptyString()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";
            var jsonResponse = "[]"; // 🔹 Simular respuesta vacía

            var restResponse = new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = jsonResponse
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakService.GetKeycloackUserIdByEmail(email, token);

            // Assert
            Assert.Equal("", result);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al obtener usuario de Keycloak"))), Times.Once);
        }


        #endregion

        #region GetKeycloakUserIdByEmail_RequestError_ReturnsEmptyString()
        [Fact]
        public async Task GetKeycloakUserIdByEmail_RequestError_ReturnsEmptyString()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";

            var restResponse = new RestResponse
            {
                StatusCode = HttpStatusCode.InternalServerError, // 🔹 Simular error 500
                ErrorMessage = "Server Error"
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakService.GetKeycloackUserIdByEmail(email, token);

            // Assert
            Assert.Equal("", result);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al obtener usuario de Keycloak"))), Times.Once);
        }


        #endregion

        #region GetKeycloakUserIdByEmail_JsonParsingError_ThrowsJsonException()
        [Fact]
        public async Task GetKeycloakUserIdByEmail_JsonParsingError_ThrowsJsonException()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";
            var invalidJsonResponse = "{ \"id\": }"; // 🔹 Sintaxis incorrecta

            var restResponse = new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = invalidJsonResponse,
                IsSuccessStatusCode = false
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(new RestResponse { StatusCode = HttpStatusCode.OK, Content = "{Invalid JSON}" });

            var keycloakServiceMock = new Mock<IKeycloakService>();

            keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new JsonException("Invalid JSON"));

            // Act & Assert
            var exception = await _keycloakService.GetKeycloackUserIdByEmail(email, token);

            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al obtener usuario de Keycloak"))), Times.Once);
        }
        #endregion

        #region GetKeycloakUserIdByEmail_UnexpectedError_ThrowsException()
        [Fact]
        public async Task GetKeycloakUserIdByEmail_UnexpectedError_ThrowsException()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _keycloakService.GetKeycloackUserIdByEmail(email, token));
            _loggerMock.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }
        #endregion


        #region GetTokenAsync_Success_ReturnsAccessToken()
        [Fact]
        public async Task GetTokenAsync_Success_ReturnsAccessToken()
        {
            // Arrange
            var accessToken = "valid-access-token";
            var jsonResponse = $"{{\"access_token\": \"{accessToken}\"}}"; // 🔹 Simular respuesta válida

            var restResponse = new RestResponse
            {
                Content = jsonResponse,
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK,
                IsSuccessStatusCode = true
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakService.GetTokenAsync();

            // Assert
            Assert.Equal(accessToken, result);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains("Access token extraído correctamente"))), Times.Once);
        }
        #endregion

        #region GetTokenAsync_RequestError_ThrowsException()
        [Fact]
        public async Task GetTokenAsync_RequestError_ThrowsException()
        {
            // Arrange
            var restResponse = new RestResponse
            {
                StatusCode = HttpStatusCode.InternalServerError, // 🔹 Simular error 500
                ErrorMessage = "Server Error"
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _keycloakService.GetTokenAsync());

            Assert.Contains("Error obteniendo token", exception.Message);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains("Error obteniendo token de Keycloak"))), Times.Once);
        }
        #endregion

        #region GetTokenAsync_MissingAccessToken_ThrowsException()
        [Fact]
        public async Task GetTokenAsync_MissingAccessToken_ThrowsException()
        {
            // Arrange
            var jsonResponse = "{}"; // 🔹 Simular respuesta sin access_token

            var restResponse = new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = jsonResponse
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _keycloakService.GetTokenAsync());

            Assert.Contains("Error inesperado al solicitar token en Keycloak:", exception.Message);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s =>
                s.Contains("Error obteniendo token de Keycloak:"))), Times.Once);
        }
        #endregion


        /*[Fact]
        public async Task GetTokenAsync_JsonParsingError_ThrowsException()
        {
            // Arrange
            var invalidJsonResponse = "{Invalid JSON}"; // 🔹 Simular JSON mal formado

            var restResponse = new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = invalidJsonResponse
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _keycloakService.GetTokenAsync());

            Assert.Contains("Error en la solicitud de token.", exception.Message);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error en la solicitud de token."))), Times.Once);
        }*/

        /*[Fact]
        public async Task GetTokenAsync_UnexpectedError_ThrowsException()
        {
            // Arrange
            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _keycloakService.GetTokenAsync());

            Assert.Contains("Error en la solicitud de token", exception.Message);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error inesperado al solicitar token en Keycloak"))), Times.Once);
        }*/


        #region AuthenticateWithKeycloak_Success_ReturnsTokens()
        [Fact]
        public async Task AuthenticateWithKeycloak_Success_ReturnsTokens()
        {
            // Arrange
            var email = "test@example.com";
            var password = "securepassword";
            var accessToken = "valid-access-token";
            var refreshToken = "valid-refresh-token";
            var expiresIn = 3600;
            var jsonResponse = $"{{\"access_token\": \"{accessToken}\", \"refresh_token\": \"{refreshToken}\", \"expires_in\": {expiresIn}}}";

            var restResponse = new RestResponse
            {
                Content = jsonResponse,
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK,
                IsSuccessStatusCode = true
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakService.AuthenticateWithKeycloak(email, password);

            // Assert
            Assert.Equal(accessToken, result.accessToken);
            Assert.Equal(refreshToken, result.refreshToken);
            Assert.Equal(expiresIn, result.expiresIn);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains($"Autenticación exitosa en Keycloak para usuario {email}"))), Times.Once);
        }
        #endregion

        #region AuthenticateWithKeycloak_RequestError_ReturnsNullTokens()
        [Fact]
        public async Task AuthenticateWithKeycloak_RequestError_ReturnsNullTokens()
        {
            // Arrange
            var email = "test@example.com";
            var password = "securepassword";

            var restResponse = new RestResponse
            {
                StatusCode = HttpStatusCode.Unauthorized, // 🔹 Simular error de autenticación
                ErrorMessage = "Unauthorized"
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakService.AuthenticateWithKeycloak(email, password);

            // Assert
            Assert.Null(result.accessToken);
            Assert.Null(result.refreshToken);
            Assert.Equal(0, result.expiresIn);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error de autenticación en Keycloak para usuario {email}"))), Times.Once);
        }
        #endregion


        #region UpdateUserPasswordInKeycloak_Success_ReturnsTrue()
        [Fact]
        public async Task UpdateUserPasswordInKeycloak_Success_ReturnsTrue()
        {
            // Arrange
            var userId = "12345";
            var password = "newSecurePassword";
            var token = "valid-token";

            var restResponse = new RestResponse
            {
                //StatusCode = HttpStatusCode.NoContent // ✅ Keycloak responde 204 cuando es exitoso
                //Content = jsonResponse,
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK,
                IsSuccessStatusCode = true
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakService.UpdateUserPasswordInKeycloak(userId, password, token);

            // Assert
            Assert.True(result);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains($"Contraseña actualizada correctamente en Keycloak para usuario {userId}"))), Times.Once);
        }
        #endregion

        #region UpdateUserPasswordInKeycloak_RequestError_ReturnsFalse()
        [Fact]
        public async Task UpdateUserPasswordInKeycloak_RequestError_ReturnsFalse()
        {
            // Arrange
            var userId = "12345";
            var password = "newSecurePassword";
            var token = "valid-token";

            var restResponse = new RestResponse
            {
                StatusCode = HttpStatusCode.BadRequest, // 🔹 Simular error de solicitud
                ErrorMessage = "Bad Request"
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakService.UpdateUserPasswordInKeycloak(userId, password, token);

            // Assert
            Assert.False(result);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains("Error al actualizar la contraseña en Keycloak"))), Times.Once);
        }
        #endregion

        /*[Fact]
        public async Task UpdateUserPasswordInKeycloak_UnexpectedError_ReturnsFalse()
        {
            // Arrange
            var userId = "12345";
            var password = "newSecurePassword";
            var token = "valid-token";

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _keycloakService.UpdateUserPasswordInKeycloak(userId, password, token);

            // Assert
            Assert.False(result);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error en la actualización de contraseña en Keycloak"))), Times.Once);
        }*/


        #region RegisterUserInKeycloak_Success_ReturnsTrue()
        [Fact]
        public async Task RegisterUserInKeycloak_Success_ReturnsTrue()
        {
            // Arrange
            var token = "valid-token";
            var request = new createUserDto
            {
                Email = "test@example.com",
                Name = "Test",
                LastName = "User",
                Password = "securepassword",
                RoleId = "user-role-id"
            };

            var restResponse = new RestResponse
            {
                StatusCode = HttpStatusCode.Created,// ✅ Keycloak responde 201 cuando el usuario se crea
                //StatusCode = HttpStatusCode.NoContent // ✅ Keycloak responde 204 cuando es exitoso
                //Content = jsonResponse,
                ResponseStatus = ResponseStatus.Completed,
                IsSuccessStatusCode = true
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakService.RegisterUserInKeycloak(request, token);

            // Assert
            Assert.True(result);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains($"Usuario {request.Email} registrado en Keycloak correctamente"))), Times.Once);
        }
        #endregion

        #region RegisterUserInKeycloak_RequestError_ReturnsFalse()
        [Fact]
        public async Task RegisterUserInKeycloak_RequestError_ReturnsFalse()
        {
            // Arrange
            var token = "valid-token";
            var request = new createUserDto
            {
                Email = "test@example.com",
                Name = "Test",
                LastName = "User",
                Password = "securepassword",
                RoleId = "user-role-id"
            };

            var restResponse = new RestResponse
            {
                StatusCode = HttpStatusCode.BadRequest, // 🔹 Simular error de solicitud
                ErrorMessage = "Bad Request"
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakService.RegisterUserInKeycloak(request, token);

            // Assert
            Assert.False(result);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains("Error al registrar usuario en Keycloak"))), Times.Once);
        }
        #endregion


        /*[Fact]
        public async Task RegisterUserInKeycloak_UnexpectedError_ReturnsFalse()
        {
            // Arrange
            var token = "valid-token";
            var request = new createUserDto
            {
                Email = "test@example.com",
                Name = "Test",
                LastName = "User",
                Password = "securepassword",
                RoleId = "user-role-id"
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _keycloakService.RegisterUserInKeycloak(request, token);

            // Assert
            Assert.False(result);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error en la comunicación con Keycloak"))), Times.Once);
        }*/

        /*[Fact]
        public async Task SendUserVerifyMail_Success_ReturnsOne()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";
            var userId = "12345";

            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token))
                .ReturnsAsync(userId);

            var restResponse = new RestResponse
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                ResponseStatus = ResponseStatus.Completed,
                IsSuccessStatusCode = true
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakServiceMock.Object.SendUserVerifyMail(email, token);

            // Assert
            //Assert.Equal(1, result);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => 
                s.Contains($"Correo de verificación enviado correctamente a {email}"))), Times.Once);
            
        }*/

        /*[Fact]
        public async Task SendUserVerifyMail_UserNotFound_ReturnsZero()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";

            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token))
                .ReturnsAsync(""); // 🔹 Simular usuario no encontrado

            // Act
            var result = await _keycloakService.SendUserVerifyMail(email, token);

            // Assert
            Assert.Equal(0, result);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Usuario no encontrado en Keycloak con email: {email}"))), Times.Once);
        }*/

        /*[Fact]
        public async Task SendUserVerifyMail_EmailRequestError_ReturnsNegativeOne()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";
            var userId = "12345";

            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token))
                .ReturnsAsync(userId);

            var restResponse = new RestResponse
            {
                StatusCode = HttpStatusCode.BadRequest, // 🔹 Simular error en el envío de email
                ErrorMessage = "Bad Request"
            };

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ReturnsAsync(restResponse);

            // Act
            var result = await _keycloakService.SendUserVerifyMail(email, token);

            // Assert
            Assert.Equal(-1, result);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al enviar correo de verificación para {email}"))), Times.Once);
        }*/

        /*[Fact]
        public async Task SendUserVerifyMail_EmailRequestUnexpectedError_ReturnsNegativeOne()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";
            var userId = "12345";

            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token))
                .ReturnsAsync(userId);

            _restClientMock.Setup(r => r.ExecuteAsync(It.IsAny<RestRequest>(), default))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _keycloakService.SendUserVerifyMail(email, token);

            // Assert
            Assert.Equal(-1, result);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains($"Error en el envío de correo de verificación para {email}"))), Times.Once);
        }*/

        /*[Fact]
        public async Task SendUserVerifyMail_UnexpectedError_ReturnsNegativeTwo()
        {
            // Arrange
            var email = "test@example.com";
            var token = "valid-token";

            _keycloakServiceMock.Setup(k => k.GetKeycloackUserIdByEmail(email, token))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _keycloakService.SendUserVerifyMail(email, token);

            // Assert
            Assert.Equal(-2, result);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains($"Error inesperado en el proceso de envío de correo de verificación para {email}"))), Times.Once);
        }*/

    }
}

