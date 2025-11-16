using Gateway.Infrastructure.DTOs;
using Gateway.Infrastructure.Exceptions;
using Gateway.Infrastructure.Interfaces;
using log4net;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;


namespace Gateway.Infrastructure.Services
{
    public class KeycloakService : IKeycloakService
    {
        private readonly IRestClient RestClient;
        private readonly ILog Logger;
        public record KeycloakTokenResponse(
            [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
            [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string RefreshToken,
            [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn,
            [property: System.Text.Json.Serialization.JsonPropertyName("scope")] string Scope
        );

        public record KeycloakUserDto(
            [property: System.Text.Json.Serialization.JsonPropertyName("id")] string Id,
            [property: System.Text.Json.Serialization.JsonPropertyName("email")] string Email
        );

        public record KeycloakRoleDto(string id, string name, bool composite);

        #region Metodos Auxiliares Privados
        /// <summary>
        /// Centraliza la construcción de peticiones a la API de Administración (Admin API) de Keycloak.
        /// </summary>
        private RestRequest CreateAdminRequest(string resource, Method method, string token)
        {
            var urlBase = Environment.GetEnvironmentVariable("KEYCLOAK_AUTHORITY_ADMIN");
            var request = new RestRequest($"{urlBase}{resource}", method);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Accept", "application/json");
            return request;
        }

        /// <summary>
        /// Centraliza la lógica de obtener la URL de token (que es diferente a la Admin API).
        /// </summary>
        private string GetTokenUrl()
        {
            return $"{Environment.GetEnvironmentVariable("KEYCLOAK_AUTHORITY")}/protocol/openid-connect/token";
        }

        /// <summary>
        /// Obtiene el ID (UUID) de un Rol de Realm por su nombre.
        /// </summary>
        private async Task<string> GetRolIDByNombre(string roleName, string token)
        {
            const string action = "Obtener Rol UUID";
            Logger.Debug($"[Keycloak:{action}] Buscando UUID para el rol: {roleName}");

            try
            {
                // Keycloak API para obtener el rol por nombre
                var request = CreateAdminRequest($"/roles/{roleName}", Method.Get, token);
                var response = await RestClient.ExecuteAsync<KeycloakRoleDto>(request);

                if (response.IsSuccessful && response.Data != null)
                {
                    return response.Data.id;
                }

                Logger.Error($"[Keycloak:{action}] ❌ No se pudo encontrar el rol {roleName}. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"[Keycloak:{action}] 🔥 Error fatal al buscar el rol {roleName}.", ex);
                // Usar una excepción controlada para propagar fallos de comunicación/lógica
                throw new KeycloakCommunicationException(ex);
            }
        }

        /// <summary>
        /// Asigna un Rol de Realm a un usuario en Keycloak.
        /// </summary>
        public async Task<bool> AsignarRolUsuario(string userId, string roleName, string token)
        {
            const string action = "Asignar Rol a Usuario";
            Logger.Info($"[Keycloak:{action}] Intentando asignar rol '{roleName}' al usuario ID: {userId}.");

            try
            {
                var roleUuid = await GetRolIDByNombre(roleName, token);
                if (string.IsNullOrEmpty(roleUuid))
                {
                    Logger.Error($"[Keycloak:{action}] ❌ No se pudo asignar el rol. UUID del rol {roleName} no encontrado.");
                    return false;
                }

                // URL para asignar Roles de Realm: /users/{userId}/role-mappings/realm
                var roleAssignmentRequest = CreateAdminRequest($"/users/{userId}/role-mappings/realm", Method.Post, token);

                var rolesToAssign = new[]
                {
                    new
                    {
                        id = roleUuid,
                        name = roleName,
                        composite = false
                    }
                };

                roleAssignmentRequest.AddJsonBody(rolesToAssign);

                var response = await RestClient.ExecuteAsync(roleAssignmentRequest);

                if (response.StatusCode == HttpStatusCode.NoContent) // 204 No Content es el éxito
                {
                    Logger.Info($"[Keycloak:{action}] ✅ Rol '{roleName}' asignado exitosamente al usuario {userId}.");
                    return true;
                }

                Logger.Error($"[Keycloak:{action}] ❌ Fallo al asignar rol {roleName} a {userId}. Status: {response.StatusCode}. Content: {response.Content}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"[Keycloak:{action}] 🔥 Error fatal no controlado durante la asignación de rol.", ex);
                return false;
            }
        }
        #endregion

        public KeycloakService(IRestClient restclient, ILog logger)
        {
            RestClient = restclient ?? throw new RestClientNullException();
            Logger = logger;
        }

        #region GetIdUsuarioKeycloakPorEmail(string correo, string token)
        /// <summary>
        /// Obtiene el ID único de Keycloak para un email dado.
        /// </summary>
        public async Task<string> GetIdUsuarioKeycloakPorEmail(string correo, string token)
        {
            try
            {
                var getUserRequest = CreateAdminRequest("/users", Method.Get, token);
                getUserRequest.AddParameter("email", correo, ParameterType.QueryString);

                //RestSharp puede deserializar directamente a una lista de DTOs.
                var getUserResponse = await RestClient.ExecuteAsync<List<KeycloakUserDto>>(getUserRequest);

                if (!getUserResponse.IsSuccessful)
                {
                    return string.Empty;
                }

                // Si la lista está vacía o es null, no se encontró el usuario.
                var userList = getUserResponse.Data;
                var userId = userList?.FirstOrDefault()?.Id;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return string.Empty;
                }

                return userId;
            }
            catch (JsonException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion

        #region GetToken()
        /// <summary>
        /// Obtiene un token de servicio (client_credentials) de Keycloak para operaciones de administración.
        /// </summary>
        // Mantiene la misma lógica
        public async Task<string> GetToken()
        {
            try
            {
                var tokenRequest = new RestRequest(GetTokenUrl(), Method.Post);
                tokenRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                tokenRequest.AddParameter("client_id", Environment.GetEnvironmentVariable("KEYCLOAK_AUDIENCE"));
                tokenRequest.AddParameter("client_secret", Environment.GetEnvironmentVariable("KEYCLOAK_SECRET"));
                tokenRequest.AddParameter("grant_type", "client_credentials");

                var tokenResponse = await RestClient.ExecuteAsync<KeycloakTokenResponse>(tokenRequest);

                if (!tokenResponse.IsSuccessful || tokenResponse.Data?.AccessToken == null)
                {
                    throw new KeycloakTokenException();
                }

                return tokenResponse.Data.AccessToken;
            }
            catch (Exception ex)
            {
                throw new KeycloakCommunicationException(ex);
            }
        }
        #endregion

        #region RegistrarUsuarioEnKeycloak(createUserDto request, string token)
        /// <summary>
        /// Registra un nuevo usuario en Keycloak (Admin API) y devuelve el ID único.
        /// </summary>
        public async Task<string> RegistrarUsuarioEnKeycloak(createUserDto request, string token)
        {
            const string action = "Registro de Usuario en Keycloak";
            // Lógica para registrar usuario y obtener el ID... (Código anterior se mantiene)

            try
            {
                var registerRequest = CreateAdminRequest("/users", Method.Post, token);
                registerRequest.AddJsonBody(new
                {
                    username = request.Correo,
                    email = request.Correo,
                    firstName = request.Nombre,
                    lastName = request.Apellido,
                    enabled = true,
                    credentials = new[]
                    {
                    new { type = "password", value = request.Contrasena, temporary = false }
                    }
                });

                var registerResponse = await RestClient.ExecuteAsync(registerRequest);

                if (registerResponse.StatusCode == HttpStatusCode.Created)
                {
                    var locationHeader = registerResponse.Headers
                        .FirstOrDefault(h => h.Name.Equals("Location", StringComparison.OrdinalIgnoreCase));

                    if (locationHeader != null && locationHeader.Value is string locationUrl)
                    {
                        var id = locationUrl.Split('/').LastOrDefault();
                        if (!string.IsNullOrEmpty(id))
                        {
                            Logger.Info($"[Keycloak:{action}] Usuario {request.Correo} registrado con ID: {id}");
                            return id;
                        }
                    }

                    Logger.Error($"[Keycloak:{action}] Registro exitoso (201), pero falló la obtención del ID en el header Location.");
                    return null;
                }
                else
                {
                    Logger.Error($"[Keycloak:{action}] Fallo al registrar {request.Correo}. Status: {registerResponse.StatusCode}. Content: {registerResponse.Content}");
                    return null;
                }
            }
            catch (Exception ex) when (ex is not KeycloakCommunicationException)
            {
                Logger.Fatal($"[Keycloak:{action}] Error fatal no controlado durante el registro.", ex);
                return null;
            }
        }
        #endregion

        #region AutenticarConKeycloak(string correo, string contrasena)
        /// <summary>
        /// Autentica un usuario y obtiene los tokens, ademas de decodificar el ID y Rol del token.
        /// Retorna (accessToken, refreshToken, expiresIn, correo, idUsuario, idRol)
        /// </summary>
        public async Task<(string accessToken, string refreshToken, int expiresIn, string email, string idUsuario, string idRol)> 
            AutenticarConKeycloak(string correo, string contrasena)
        {
            const string action = "Autenticación de Usuario";
            Logger.Info($"[Keycloak:{action}] Iniciando proceso para el usuario: {correo}");

            try
            {
                var clientId = Environment.GetEnvironmentVariable("KEYCLOAK_AUDIENCE");
                var clientSecret = Environment.GetEnvironmentVariable("KEYCLOAK_SECRET");

                var basicAuthValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

                var loginRequest = new RestRequest(GetTokenUrl(), Method.Post);

                loginRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                loginRequest.AddHeader("Authorization", $"Basic {basicAuthValue}");
                loginRequest.AddParameter("grant_type", "password");
                loginRequest.AddParameter("username", correo);
                loginRequest.AddParameter("password", contrasena);

                Logger.Debug($"[Keycloak:{action}] Solicitando tokens de acceso/refresco a Keycloak para {correo}.");

                var loginResponse = await RestClient.ExecuteAsync<KeycloakTokenResponse>(loginRequest);

                if (!loginResponse.IsSuccessful || loginResponse.Data?.AccessToken == null)
                {
                    Logger.Error($"[Keycloak:{action}] Fallo al obtener tokens para {correo}. Status: " +
                                 $"{loginResponse.StatusCode}. Contenido: {loginResponse.Content}");
                    return (null, null, 0, null, null, null);
                }

                var data = loginResponse.Data;
                Logger.Debug($"[Keycloak:{action}] Tokens obtenidos exitosamente para {correo}. Tiempo de expiración (s): {data.ExpiresIn}.");

                //Extracción de ID y Rol del Access Token
                var handler = new JwtSecurityTokenHandler();

                //La decodificación del JWT puede fallar si el token no es válido o está mal formado.
                if (!handler.CanReadToken(data.AccessToken))
                {
                    Logger.Error($"[Keycloak:{action}] AccessToken no es un JWT válido o está mal formado para {correo}.");
                    return (null, null, 0, null, null, null);
                }

                var jwtToken = handler.ReadJwtToken(data.AccessToken);

                //Extracción de ID de Usuario
                string idUsuario = null;

                // 1. Intenta obtener el ID de usuario estándar
                idUsuario = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                if (string.IsNullOrEmpty(idUsuario))
                {
                    Logger.Warn($"[Keycloak:{action}] ID de Usuario (sub) NO encontrado. Intentando usar 'jti' (ID del Token) como fallback.");
                    // 2. Fallback: Usar JTI (ID del JWT). En Keycloak, este claim suele ser 'jti'
                    idUsuario = jwtToken.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;

                    if (string.IsNullOrEmpty(idUsuario))
                    {
                        Logger.Warn($"[Keycloak:{action}] ID de Token (jti) NO encontrado. Intentando usar 'preferred_username' (o 'email').");
                        // 3. Fallback: Usar preferred_username o email
                        idUsuario = jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
                        if (string.IsNullOrEmpty(idUsuario))
                        {
                            idUsuario = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                        }
                    }
                }

                Logger.Debug($"[Keycloak:{action}] ID de Usuario extraído ({(string.IsNullOrEmpty(idUsuario) ? "NULL" : "OK")}): {idUsuario ?? "NULL"}.");

                string idRol = null;
                var noiseRoles = new[] { "offline_access", "uma_authorization", "default-roles-" };

                // 1. Buscar en el CLAIM 'roles' (Tu configuración específica de Client Scope que inyecta roles como claims individuales)
                Logger.Debug($"[Keycloak:{action}] Buscando rol en claims individuales de tipo 'roles'.");

                var actualRoles = jwtToken.Claims
                    // Filtrar por el tipo de claim "roles"
                    .Where(c => c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase))
                    // Obtener los valores (los nombres de rol)
                    .Select(c => c.Value)
                    // Excluir los roles de sistema/ruido
                    .Where(v => !noiseRoles.Any(n => v.StartsWith(n, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (actualRoles.Any())
                {
                    idRol = actualRoles.First();
                    Logger.Debug($"[Keycloak:{action}] Rol principal encontrado en claims individuales 'roles': {idRol}. Roles completos: {string.Join(", ", actualRoles)}.");
                }

                // Función auxiliar para extraer el primer rol de un claim JSON (para fallbacks)
                string ExtractFirstRoleFromJsonObject(Claim claim, string claimName, string clientAudience)
                {
                    if (claim == null) return null;

                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(claim.Value))
                        {
                            var roles = Enumerable.Empty<string>();
                            // Manejo para Realm Roles (claim: realm_access, property: roles)
                            if (claimName == "realm_access" &&
                                doc.RootElement.TryGetProperty("roles", out JsonElement rolesElement) &&
                                rolesElement.ValueKind == JsonValueKind.Array)
                            {
                                roles = rolesElement.EnumerateArray()
                                    .Select(r => r.GetString())
                                    .Where(r => r != null)
                                    .ToList();
                            }

                            // Manejo para Client Roles (claim: resource_access, property: [client_id].roles)
                            else if (claimName == "resource_access" &&
                                doc.RootElement.TryGetProperty(clientAudience, out JsonElement clientRolesElement))
                            {
                                if (clientRolesElement.TryGetProperty("roles", out JsonElement rolesElementClient) &&
                                    rolesElementClient.ValueKind == JsonValueKind.Array)
                                {
                                    roles = rolesElementClient.EnumerateArray()
                                        .Select(r => r.GetString())
                                        .Where(r => r != null)
                                        .ToList();

                                }
                            }
                            var actualRoles = roles.Where(r => !noiseRoles.Any(n => r.StartsWith(n, StringComparison.OrdinalIgnoreCase)))
                                .ToList();

                            if (actualRoles.Any())
                            {
                                return actualRoles.First();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[Keycloak:{action}] Error al parsear el claim '{claimName}' para el rol.", ex);
                    }
                    return null;
                }

                if (string.IsNullOrEmpty(idRol))
                {
                    Logger.Debug($"[Keycloak:{action}] Rol no encontrado en 'roles' individual. Buscando en realm_access.");
                    var realmAccessClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "realm_access");
                    idRol = ExtractFirstRoleFromJsonObject(realmAccessClaim, "realm_access", clientId);
                }

                // 3. Si no se encuentra, intentar obtener el rol de Client Access
                if (string.IsNullOrEmpty(idRol))
                {
                    Logger.Debug($"[Keycloak:{action}] Rol no encontrado en realm_access. Buscando en resource_access.");
                    var resourceAccessClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "resource_access");
                    idRol = ExtractFirstRoleFromJsonObject(resourceAccessClaim, "resource_access", clientId);
                }

                // 4. Si aún no se encuentra, busca en el scope (último fallback)
                if (string.IsNullOrEmpty(idRol))
                {
                    var scopeRoles = data.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                    .Where(s => !noiseRoles.Any(n => s.StartsWith(n, StringComparison.OrdinalIgnoreCase)) && s != "openid" && s != "email" && s != "profile")
                                    .ToList();
                    idRol = scopeRoles?.FirstOrDefault();

                    Logger.Debug($"[Keycloak:{action}] Rol principal no encontrado en claims. Revisando Scope. Roles en Scope: {string.Join(", ", scopeRoles ?? new List<string>())}");
                }

                // Si el rol es nulo o vacío, registra la advertencia
                if (string.IsNullOrEmpty(idRol))
                {
                    Logger.Warn($"[Keycloak:{action}] No se pudo determinar el rol principal del token para el usuario {correo}.");
                }
                else
                {
                    Logger.Info($"[Keycloak:{action}] ✅ Usuario autenticado y datos extraídos: ID={idUsuario}, Rol={idRol}.");
                }

                Logger.Info($"[Keycloak:{action}] Usuario autenticado y datos extraídos: ID={idUsuario}, Rol={idRol}.");
                return (data.AccessToken, data.RefreshToken, data.ExpiresIn, correo, idUsuario, idRol);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Keycloak:{action}] Error inesperado durante la autenticación.", ex);
                return (null, null, 0, null, null, null);
            }
        }
        #endregion

        #region SendCorreoVerificacion(string correo, string token)
        /// <summary>
        /// Dispara el flujo de envío de correo de verificación de Keycloak para un usuario.
        /// </summary>
        public async Task<bool> SendCorreoVerificacion(string correo, string token)
        {
            const string action = "Envío de Correo de Verificación";

            var id = await GetIdUsuarioKeycloakPorEmail(correo, token);
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            try
            {
                var verifyEmailUrl = $"/users/{id}/send-verify-email";
                var emailRequest = CreateAdminRequest(verifyEmailUrl, Method.Put, token);

                var emailResponse = await RestClient.ExecuteAsync(emailRequest);

                if (!emailResponse.IsSuccessful)
                {
                    throw new KeycloakCommunicationMailException();
                }

                return true;
            }
            catch (KeycloakCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("Error inesperado en la infraestructura de Keycloak.", ex);
            }
        }
        #endregion

        #region ActualizarContrasenaEnKeycloak(string idUsuario, string contrasena, string token)
        /// <summary>
        /// Actualiza la contraseña de un usuario en Keycloak (Admin API).
        /// </summary>
        public async Task<bool> ActualizarContrasenaEnKeycloak(string idUsuario, string contrasena, string token)
        {
            const string action = "Actualización de Contraseña";

            try
            {
                Logger.Info($"[Keycloak:{action}] Intentando actualizar contraseña para el ID: {idUsuario}.");

                var request = CreateAdminRequest($"/users/{idUsuario}/reset-password", Method.Put, token);
                request.AddHeader("Content-Type", "application/json");

                // DTO anónimo para la carga útil
                request.AddJsonBody(new
                {
                    type = "password",
                    value = contrasena,
                    temporary = false
                });

                var response = await RestClient.ExecuteAsync(request);

                // Reemplazamos HandleAdminResponse con la verificación directa:
                if (response.StatusCode == HttpStatusCode.NoContent) // Keycloak devuelve 204 No Content en caso de éxito
                {
                    Logger.Info($"[Keycloak:{action}] ✅ Contraseña actualizada exitosamente para el ID: {idUsuario}.");
                    return true;
                }
                else
                {
                    Logger.Error($"[Keycloak:{action}] ❌ Fallo al actualizar contraseña para el ID: {idUsuario}. Status: {response.StatusCode}. Content: {response.Content}");
                    // Lanzar una excepción específica si la actualización falla por razones de Keycloak
                    // Esto permite que el controlador maneje el error de manera adecuada (ej. BadRequest)
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"[Keycloak:{action}] 🔥 Error fatal no controlado durante la actualización de contraseña.", ex);
                return false;
            }
        }
        #endregion


    }
}
