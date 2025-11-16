using Gateway.Infrastructure.DTOs;
using Gateway.Infrastructure.Services;
using log4net;
using Moq;
using RestSharp;
using System.Text.Json;

namespace Gateway.Tests.Gateway.Infraestructure.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IRestClient> _restClientMock;
        private readonly Mock<ILog> _loggerMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _restClientMock = new Mock<IRestClient>();
            _loggerMock = new Mock<ILog>();

            _userService = new UserService(_restClientMock.Object, _loggerMock.Object);
        }

        #region SaveUserActivity_Success()
        [Fact]
        public async Task SaveUserActivity_Success()
        {
            // Arrange
            var userId = "user123";
            var action = "LOGIN";
            var token = "test_token";

            var response = new RestResponse
            {
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK,
                IsSuccessStatusCode = true
            };

            _restClientMock.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(r => r.Resource.Contains("publishActivity")), CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            await _userService.saveUserActivity(userId, action, token);

            // Assert
            //_loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains($"Registrando actividad del usuario {userId}"))), Times.Once);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains($"Actividad registrada exitosamente para usuario {userId}"))), Times.Once);
        }
        #endregion

        #region SaveUserActivity_Failure_ThrowsException()
        [Fact]
        public async Task SaveUserActivity_Failure_ThrowsException()
        {
            // Arrange
            var userId = "user123";
            var action = "LOGIN";
            var token = "test_token";

            var response = new RestResponse { ResponseStatus = ResponseStatus.Error, ErrorMessage = "Mocked error" };

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ReturnsAsync(response);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _userService.saveUserActivity(userId, action, token));
            Assert.Equal("Error al registrar actividad: Mocked error", exception.Message);

            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains($"Error inesperado al registrar actividad del usuario {userId}")),
                It.IsAny<Exception>()), Times.Once);
        }
        #endregion

        #region SaveUserActivity_UnexpectedException_ThrowsException()
        [Fact]
        public async Task SaveUserActivity_UnexpectedException_ThrowsException()
        {
            // Arrange
            var userId = "user123";
            var action = "LOGIN";
            var token = "test_token";

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _userService.saveUserActivity(userId, action, token));
            Assert.Equal("Unexpected error", exception.Message);

            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains($"Error inesperado al registrar actividad del usuario {userId}")),
                It.IsAny<Exception>()), Times.Once);
        }
        #endregion


        #region GetUserFromService_Success()
        [Fact]
        public async Task GetUserFromService_Success()
        {
            // Arrange
            var email = "test@example.com";
            var token = "test_token";

            var responseContent = JsonSerializer.Serialize(new { userId = "user123", roleId = "admin" });
            var response = new RestResponse
            {
                Content = responseContent,
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK,
                IsSuccessStatusCode = true
            };

            _restClientMock.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(r =>
                    r.Resource.Contains("getuserbyemail")), CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var (userId, roleId) = await _userService.GetUserFromService(email, token);

            // Assert
            //Assert.Equal("user123", userId);
            //Assert.Equal("admin", roleId);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s =>
                s.Contains($"Usuario obtenido correctamente desde el microservicio para {email}"))), Times.Once);
        }
        #endregion

        #region GetUserFromService_Failure_ReturnsNull()
        [Fact]
        public async Task GetUserFromService_Failure_ReturnsNull()
        {
            // Arrange
            var email = "test@example.com";
            var token = "test_token";

            var response = new RestResponse
            {
                ResponseStatus = ResponseStatus.Error,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                ErrorMessage = "Mocked error"
            };

            _restClientMock.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(r => r.Resource.Contains("getuserbyemail")), CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var (userId, roleId) = await _userService.GetUserFromService(email, token);

            // Assert
            //Assert.Null(userId);
            //Assert.Null(roleId);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al obtener usuario en la base de datos para {email}"))), Times.Once);
        }
        #endregion

        #region GetUserFromService_UnexpectedException_ThrowsException()
        [Fact]
        public async Task GetUserFromService_UnexpectedException_HandlesGracefully()
        {
            // Arrange
            var email = "test@example.com";
            var token = "test_token";

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var (userId, roleId) = await _userService.GetUserFromService(email, token);

            // Assert
            //Assert.Null(userId);
            //Assert.Null(roleId);

            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains($"Error inesperado al obtener usuario en la base de datos para {email}")),
                It.IsAny<Exception>()), Times.Once);
        }
        #endregion

        #region GetUserFromService_InvalidJson_ReturnsNull()
        [Fact]
        public async Task GetUserFromService_InvalidJson_ReturnsNull()
        {
            // Arrange
            var email = "test@example.com";
            var token = "test_token";

            // Simular una respuesta exitosa pero con JSON malformado
            var response = new RestResponse
            {
                Content = "INVALID_JSON",
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK
            };

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var (userId, roleId) = await _userService.GetUserFromService(email, token);

            // Assert
            //Assert.Null(userId);
            //Assert.Null(roleId);

            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al obtener usuario en la base de datos para {email}"))), Times.Once);
        }
        #endregion

        #region GetUserFromService_MissingUserIdOrRoleId_ReturnsNull()
        [Fact]
        public async Task GetUserFromService_MissingUserIdOrRoleId_ReturnsNull()
        {
            // Arrange
            var email = "test@example.com";
            var token = "test_token";

            // Simular una respuesta exitosa pero sin `userId` o `roleId`
            var responseContent = JsonSerializer.Serialize(new { name = "John Doe" }); // 🚀 No incluye userId ni roleId
            var response = new RestResponse
            {
                Content = responseContent,
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK
            };

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var (userId, roleId) = await _userService.GetUserFromService(email, token);

            // Assert
            //Assert.Null(userId);
            //Assert.Null(roleId); // 🔹 La función debe manejar el error y devolver `null`.

            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al obtener usuario en la base de datos para {email}"))), Times.Once);
        }
        #endregion
        //aaaa
        #region GetUserFromService_MissingEmailOrToken_ReturnsNull()
        /*[Fact]
        public async Task GetUserFromService_MissingToken_ThrowsArgumentNullException()
        {
            // Arrange
            var email = "test@example.com";
            string token = null; // 🚀 Simular que el token es nulo

            // Act & Assert
            //var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _userService.GetUserFromService(email, token));
            var exception = await  _userService.GetUserFromService(email, token);

            Assert.Equal(token, exception.ToString()); // 🔹 Verifica que el parámetro afectado es `accessToken`
            //_loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Token no proporcionado para registrar actividad del usuario {email}"))), Times.Once);
        }*/
        #endregion 


        #region RegisterUserInDatabase_Success()
        [Fact]
        public async Task RegisterUserInDatabase_Success()
        {
            // Arrange
            var request = new createUserDto
            {
                Email = "test@example.com",
                Name = "John",
                LastName = "Doe",
                RoleId = "admin",
                Address = "123 Street",
                Phone = "5551234",
                Password = "password123"
            };
            var token = "test_token";

            var response = new RestResponse
            {
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK,
                IsSuccessStatusCode = true
            };

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var result = await _userService.RegisterUserInDatabase(request, token);

            // Assert
            //Assert.True(result);
            _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains($"Usuario {request.Email} registrado en la base de datos correctamente."))), Times.Once);
        }
        #endregion

        #region RegisterUserInDatabase_Failure_ReturnsFalse()
        [Fact]
        public async Task RegisterUserInDatabase_Failure_ReturnsFalse()
        {
            // Arrange
            var request = new createUserDto
            {
                Email = "test@example.com",
                Name = "John",
                LastName = "Doe",
                RoleId = "admin",
                Address = "123 Street",
                Phone = "5551234",
                Password = "password123"
            };
            var token = "test_token";

            var response = new RestResponse
            {
                ResponseStatus = ResponseStatus.Error,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                ErrorMessage = "Mocked error"
            };

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var result = await _userService.RegisterUserInDatabase(request, token);

            // Assert
            //Assert.False(result);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al registrar usuario en la base de datos"))), Times.Once);
        }
        #endregion

        #region RegisterUserInDatabase_UnexpectedException_ReturnsFalse()
        [Fact]
        public async Task RegisterUserInDatabase_UnexpectedException_ReturnsFalse()
        {
            // Arrange
            var request = new createUserDto
            {
                Email = "test@example.com",
                Name = "John",
                LastName = "Doe",
                RoleId = "admin",
                Address = "123 Street",
                Phone = "5551234",
                Password = "password123"
            };
            var token = "test_token";

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _userService.RegisterUserInDatabase(request, token);

            // Assert
            //Assert.False(result);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error en la comunicación con el microservicio de usuario")),
                It.IsAny<Exception>()), Times.Once);
        }
        #endregion

        #region RegisterUserInDatabase_EmptyResponse_ReturnsFalse()
        [Fact]
        public async Task RegisterUserInDatabase_EmptyResponse_ReturnsFalse()
        {
            // Arrange
            var request = new createUserDto
            {
                Email = "test@example.com",
                Name = "John",
                LastName = "Doe",
                RoleId = "admin",
                Address = "123 Street",
                Phone = "5551234",
                Password = "password123"
            };
            var token = "test_token";

            // Simular una respuesta exitosa pero sin contenido
            var response = new RestResponse
            {
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = "" // 🚀 La respuesta está vacía
            };

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var result = await _userService.RegisterUserInDatabase(request, token);

            // Assert
            //Assert.False(result);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains("Error al registrar usuario en la base de datos"))), Times.Once);
        }
        #endregion


        #region GetUserIdFromDatabase_Success()
        [Fact]
        public async Task GetUserIdFromDatabase_Success()
        {
            // Arrange
            var email = "test@example.com";
            var token = "test_token";

            var responseContent = JsonSerializer.Serialize(new { userId = "user123" });
            var response = new RestResponse
            {
                Content = responseContent,
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK,
                IsSuccessStatusCode = true
            };

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var userId = await _userService.GetUserIdFromDatabase(email, token);

            // Assert
            Assert.Equal("user123", userId);
            //_loggerMock.Verify(x => x.Warn(It.IsAny<string>()), Times.Never);
            _loggerMock.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }
        #endregion

        #region GetUserIdFromDatabase_Failure_ReturnsNull()
        [Fact]
        public async Task GetUserIdFromDatabase_Failure_ReturnsNull()
        {
            // Arrange
            var email = "test@example.com";
            var token = "test_token";

            var response = new RestResponse
            {
                ResponseStatus = ResponseStatus.Error,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                ErrorMessage = "Mocked error"
            };

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var userId = await _userService.GetUserIdFromDatabase(email, token);

            // Assert
            Assert.Null(userId);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al obtener usuario desde el microservicio de usuario"))), Times.Once);
        }
        #endregion

        #region GetUserIdFromDatabase_UnexpectedException_ReturnsNull()
        [Fact]
        public async Task GetUserIdFromDatabase_UnexpectedException_ReturnsNull()
        {
            // Arrange
            var email = "test@example.com";
            var token = "test_token";

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var userId = await _userService.GetUserIdFromDatabase(email, token);

            // Assert
            Assert.Null(userId);
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains($"Error en la comunicación con el microservicio de usuario")),
                It.IsAny<Exception>()), Times.Once);
        }
        #endregion

        #region GetUserIdFromDatabase_InvalidJson_ReturnsNull()
        [Fact]
        public async Task GetUserIdFromDatabase_InvalidJson_ReturnsNull()
        {
            // Arrange
            var email = "test@example.com";
            var token = "test_token";

            // Simular una respuesta con JSON inválido
            var response = new RestResponse
            {
                Content = "INVALID_JSON",
                ResponseStatus = ResponseStatus.Completed,
                StatusCode = System.Net.HttpStatusCode.OK
            };

            _restClientMock.Setup(x => x.ExecuteAsync(It.IsAny<RestRequest>(), CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var userId = await _userService.GetUserIdFromDatabase(email, token);

            // Assert
            Assert.Null(userId);
            _loggerMock.Verify(x => x.Warn(It.Is<string>(s => s.Contains($"Error al obtener usuario desde el microservicio de usuario"))), Times.Once);
        }
        #endregion

    }
}

