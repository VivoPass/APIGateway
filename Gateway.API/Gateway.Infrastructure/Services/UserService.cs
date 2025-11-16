using Gateway.Infrastructure.DTOs;
using Gateway.Infrastructure.Exceptions;
using Gateway.Infrastructure.Interfaces;
using log4net;
using RestSharp;
using System.Text.Json;


namespace Gateway.Infrastructure.Services
{
    public class UserService: IUserService
    {
        private readonly IRestClient RestClient;
        private const string BASE_URL = "http://localhost:5183/api/Usuarios"; // URL Base centralizada
        private readonly ILog Logger;
        public UserService(IRestClient restclient, ILog logger)
        {
            RestClient = restclient ?? throw new RestClientNullException();
            Logger = logger;
        }

        #region Metodos Auxiliares Privados (DRY)

        public record UserDataResponseDto(
            // Identificador único del usuario en el sistema de negocio (DB)
            [property: System.Text.Json.Serialization.JsonPropertyName("userId")]
            string UserId,

            // Identificador del rol o nivel de acceso del usuario
            [property: System.Text.Json.Serialization.JsonPropertyName("roleId")]
            string RoleId,

            // (Opcional, pero útil) El correo para trazabilidad y logging
            [property: System.Text.Json.Serialization.JsonPropertyName("email")]
            string Email
        );

        /// <summary>
        /// Construye una RestRequest con el Bearer Token y Content-Type JSON.
        /// </summary>
        private RestRequest CreateServiceRequest(string endpoint, Method method, string token)
        {
            var url = $"{BASE_URL}{endpoint}";
            var request = new RestRequest(url, method);
            request.AddHeader("Authorization", $"Bearer {token}");
           // request.AddHeader("Content-Type", "application/json");
            Logger.Debug($"Creando Request: {method} {url}");

            return request;
        }

        /// <summary>
        /// Maneja la ejecución de la request y lanza una excepción ante fallo.
        /// </summary>
        private async Task ExecuteAndThrowIfFailed(RestRequest request, string action)
        {
            Logger.Info($"[UserService:{action}] Ejecutando request hacia el microservicio.");
            var response = await RestClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                Logger.Error($"[UserService:{action}] Fallo en la comunicación con el microservicio. URL: " +
                             $"{request.Resource}. Status: {response.StatusCode}. Content: {response.Content}");
                throw new UserServiceException();
            }
            Logger.Debug($"[UserService:{action}] Ejecución de request exitosa.");
        }

        #endregion


        #region SaveActividadUsuario(string idUsuario, string accion, string token)
        /// <summary>
        /// Publica un evento de actividad del usuario en el microservicio (asíncrono).
        /// </summary>
        public async Task SaveActividadUsuario(string idUsuario, string accion, string token)
        {
            const string action = "Registro de Actividad de Usuario";
            Logger.Info($"[UserService:{action}] Intentando registrar actividad '{accion}' para usuario ID: {idUsuario}.");

            try
            {
                var APIRequest = CreateServiceRequest("/publishActivity", Method.Post, token);

                APIRequest.AddJsonBody(new
                {
                    userId = idUsuario,
                    action = accion,
                });

                await ExecuteAndThrowIfFailed(APIRequest, action);
                Logger.Info($"[UserService:{action}] ✅ Actividad '{accion}' registrada exitosamente.");

            }
            catch (Exception ex)
            {
                Logger.Error($"[UserService:{action}] ❌ Fallo al registrar actividad '{accion}' para {idUsuario}.", ex);
                throw;
            }
        }
        #endregion

        #region GetUsuarioDelService(string correo, string accessToken)
        /// <summary>
        /// Obtiene el ID de usuario y rol desde el microservicio por email.
        /// </summary>
        public async Task<(string idUsuario, string idRol)> GetUsuarioDelService(string correo, string accessToken)
        {
            const string action = "Obtener Usuario por Email";

            if (string.IsNullOrWhiteSpace(correo))
            {
                Logger.Error($"[UserService:{action}] Argumento nulo o vacío: {nameof(correo)}.");
                throw new ArgumentNullException(nameof(correo));
            }
            Logger.Info($"[UserService:{action}] Buscando usuario por correo: {correo}");

            try
            {
                // Reutilización de método auxiliar y endpoint más legible
                var getUserAPIRequest = CreateServiceRequest("/getUsuarioByCorreo", Method.Get, accessToken);
                getUserAPIRequest.AddParameter("email", correo, ParameterType.QueryString);

                //RestSharp genérico para deserializar directamente al DTO
                var getUserAPIResponse = await RestClient.ExecuteAsync<UserDataResponseDto>(getUserAPIRequest);

                if (!getUserAPIResponse.IsSuccessful)
                {
                    Logger.Error($"[UserService:{action}] Fallo en la comunicación o respuesta de servicio. Status: " +
                                 $"{getUserAPIResponse.StatusCode}. Content: {getUserAPIResponse.Content}");
                    return (null, null);
                }

                if (getUserAPIResponse.Data?.UserId == null)
                {
                    Logger.Warn($"[UserService:{action}] Usuario {correo} no encontrado en Microservicio o datos incompletos.");
                    return (null, null);
                }

                var userData = getUserAPIResponse.Data;
                Logger.Info($"[UserService:{action}] Usuario encontrado. ID: {userData.UserId}, Rol: {userData.RoleId}");

                return (userData.UserId, userData.RoleId);
            }
            catch (Exception ex)
            {
                Logger.Error($"[UserService:{action}] Excepción inesperada al buscar usuario {correo}.", ex);
                return (null, null); // Retorna nulo ante cualquier fallo inesperado
            }
        }
        #endregion

        #region GetIDUsuarioDeBD(string correo, string token)
        /// <summary>
        /// Obtiene solo el ID del usuario desde la base de datos por email.
        /// </summary>
        public async Task<string> GetIDUsuarioDeBD(string correo, string token)
        {
            // Si necesitas solo el ID, es mejor llamar al método existente:
            var (idUsuario, _) = await GetUsuarioDelService(correo, token);
            return idUsuario;

            /*
            // Si tienes que mantenerlo como una llamada separada:
            const string action = "Obtener ID de Usuario";

            try
            {
                 // Usamos la misma lógica que GetUsuarioDelService pero solo extraemos el ID
                 var getUserAPIRequest = CreateServiceRequest("/getuserbyemail", Method.Get, token);
                 getUserAPIRequest.AddParameter("email", correo, ParameterType.QueryString);

                 var getUserAPIResponse = await _restClient.ExecuteAsync<UserDataResponseDto>(getUserAPIRequest);

                 if (!getUserAPIResponse.IsSuccessful || getUserAPIResponse.Data?.UserId == null)
                 {
                     return null;
                 }

                 return getUserAPIResponse.Data.UserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ID de usuario {Email}", correo);
                return null;
            }
            */
        }
        #endregion

    }
}
