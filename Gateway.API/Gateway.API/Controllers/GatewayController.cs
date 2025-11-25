using Gateway.API.DTOs;
using Gateway.Infrastructure.DTOs;
using Gateway.Infrastructure.Interfaces;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestSharp;
using System.Net;
using System.Security.Claims;

namespace Gateway.API.Controllers
{
    /// <summary>
    /// Controlador principal que actúa como API Gateway, gestionando la autenticación (Login, Registro, Contraseña)
    /// y enrutando peticiones a los microservicios internos, principalmente Keycloak.
    /// </summary>
    [ApiController]
    [Route("/")]
    public class GatewayController : ControllerBase
    {
        private readonly IRestClient _restClient;
        private readonly ISendEmailService _sendEmailService;
        private readonly IKeycloakService _keycloakService;
        private readonly ILog Logger;
        private readonly IAuditoriaRepository AuditoriaRepository;

        public GatewayController(IRestClient restClient, ISendEmailService sendEmailService, IKeycloakService keycloakService, ILog logger, IAuditoriaRepository auditoriaRepository)
        {
            _restClient = restClient;
            _sendEmailService = sendEmailService;
            _keycloakService = keycloakService;
            Logger = logger;
            AuditoriaRepository = auditoriaRepository;
        }

        #region Login
        /// <summary>
        /// Permite a un usuario autenticarse en el sistema.
        /// Procesa la autenticación en Keycloak y publica una actividad en el MS de Usuarios.
        /// </summary>
        /// <param name="request">Datos de inicio de sesión (correo y contraseña).</param>
        /// <returns>Objeto con tokens de acceso y datos de usuario si es exitoso (200 OK).</returns>
        [HttpPost("login")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(object))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Login([FromBody] loginDto request)
        {
            Logger.Info($"Iniciando intento de login para el correo: {request.Correo}");
            try
            {
                Logger.Debug($"Autenticando usuario {request.Correo} en Keycloak.");
                var (accessToken, refreshToken, expiresIn, email, idUsuario, idRol) =
                    await this._keycloakService.AutenticarConKeycloak(request.Correo, request.Contrasena);

                if (string.IsNullOrEmpty(accessToken))
                {
                    Logger.Error($"Autenticación fallida para {request.Correo}. Keycloak devolvió credenciales inválidas o no respondió.");
                    return BadRequest("Credenciales inválidas. Error de autenticación en Keycloak.");
                }

                if (string.IsNullOrEmpty(idUsuario))
                {
                    Logger.Error($"Autenticación fallida para {request.Correo}. No se pudo obtener el ID de usuario del token JWT.");
                    return StatusCode(500, "Error de configuración: No se pudo obtener el ID del usuario.");
                }

                Logger.Info($"Usuario {request.Correo} autenticado con éxito. ID: {idUsuario}, Rol: {idRol}.");

                //Conexion con el MS de Usuarios para la publicacion de la actividad
                Logger.Debug($"[MS-USUARIOS] Preparando publicación de actividad para usuario: {idUsuario}.");
                var requestUsuario = new RestRequest(Environment.GetEnvironmentVariable("USUARIOS_MS_URL") +
                                                     $"/publishActivity", Method.Post);
                var activityBody = new
                    { IdUsuario = idUsuario, accion = $"Log in." };
                requestUsuario.AddJsonBody(activityBody);

                Logger.Debug($"[MS-USUARIOS] Enviando POST a {requestUsuario}. Payload: {{ userId: {idUsuario}, activityType: 'UserLogIn' }}");
                var responseUsuario = await _restClient.ExecuteAsync(requestUsuario);
                if (!responseUsuario.IsSuccessful)
                {
                    Logger.Error($"[500] ERROR DE COMUNICACIÓN EXTERNA [MS-USUARIOS]: El registro del Log in del usuario {idUsuario} fue exitoso, " +
                                 $"pero falló la publicación de la actividad del usuario {idUsuario}." +
                                 $" StatusCode: {responseUsuario.StatusCode}, Content: {responseUsuario.Content}",
                        responseUsuario.ErrorException);
                    return StatusCode(500, "Error al completar la publicación de la actividad del usuario");
                }
                Logger.Info($"[MS-USUARIOS] Actividad de usuario {idUsuario} publicada exitosamente. " +
                            $"Status: {responseUsuario.StatusCode}.");

                await AuditoriaRepository.InsertarAuditoriaGateway(idUsuario, "INFO", "USUARIO_LOGIN", 
                    $"El usuario con ID '{idUsuario}' acaba de iniciar sesión en el sistema.");


                Logger.Info($"Login completado exitosamente para {request.Correo}. Retornando respuesta (200 OK).");
                return Ok(new
                {
                    access_token = accessToken,
                    refresh_token = refreshToken,
                    expires_in = expiresIn,
                    email = request.Correo,
                    UserId = idUsuario,
                    roleId = idRol
                });
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Error fatal (500) no controlado durante el proceso de login para {request.Correo}.", ex);
                return StatusCode(500, "Error interno en el servidor.");
            }
        }
        #endregion

        #region Register
        /// <summary>
        /// Registra un nuevo usuario en Keycloak, le asigna un rol y envía un correo de verificación.
        /// </summary>
        /// <param name="request">Datos del nuevo usuario (correo, contraseña, rol).</param>
        /// <returns>ID del usuario creado (201 Created) o error (400 Bad Request / 500 Internal Server Error).</returns>
        [HttpPost("register")]
        [ProducesResponseType((int)HttpStatusCode.Created, Type = typeof(object))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Register([FromBody] createUserDto request)
        {
            Logger.Info($"Iniciando registro para el usuario: {request.Correo}");
            string idKeycloak = null;
            string idRol = request.idRol;

            try
            {
                Logger.Debug("Intentando obtener token de servicio para Keycloak.");
                var token = await this._keycloakService.GetToken();
                Logger.Debug("Token de Keycloak obtenido.");

                Logger.Info($"Registrando usuario {request.Correo} en Keycloak.");
                idKeycloak = await this._keycloakService.RegistrarUsuarioEnKeycloak(request, token);
                if (idKeycloak == null)
                {
                    Logger.Error($"Falló el registro de {request.Correo} en Keycloak.");
                    return BadRequest("Error al registrar usuario en Keycloak.");
                }
                Logger.Info($"Usuario {request.Correo} registrado exitosamente en Keycloak. ID: {idKeycloak}.");

                Logger.Info($"Asignando rol '{idRol}' al usuario {idKeycloak}.");
                if (!await this._keycloakService.AsignarRolUsuario(idKeycloak, idRol, token))
                {
                    Logger.Error($"Falló la asignación del rol '{idRol}' al usuario {idKeycloak}.");
                    return BadRequest($"Error al asignar el rol {idRol} al usuario.");
                }
                Logger.Info($"Rol '{idRol}' asignado con éxito.");

                Logger.Info($"Enviando correo de verificación a: {request.Correo}");
                if (!await this._keycloakService.SendCorreoVerificacion(request.Correo, token))
                {
                    Logger.Warn($"Falló el envío del correo de verificación a {request.Correo}.");
                }
                Logger.Info($"Correo de verificación enviado (o intento finalizado).");

                Logger.Info($"Proceso de registro completado exitosamente para: {request.Correo}. Retornando Ok (201).");

                await AuditoriaRepository.InsertarAuditoriaGateway(idKeycloak, "INFO", "USUARIO_REGISTER",
                    $"El usuario con ID '{idKeycloak}' acaba de registrarse en el sistema.");

                return CreatedAtAction(nameof(Register), new { id = idKeycloak }, new
                {
                    idusuario = idKeycloak,
                    idrol = idRol // Se devuelve el rol que se intentó asignar
                });

            }
            catch (Exception ex)
            {
                Logger.Fatal($"Error fatal (500) no controlado durante el registro de {request.Correo}.", ex);
                return StatusCode(500, "Error interno en el servidor.");
            }
        }
        #endregion

        #region Reset-Password (Olvidar contraseña)
        /// <summary>
        /// Inicia el proceso de restablecimiento de contraseña enviando un correo con un enlace a Keycloak.
        /// </summary>
        /// <param name="email">El correo electrónico del usuario.</param>
        /// <returns>200 OK si el correo fue enviado, o error 400/500.</returns>
        [HttpPost("reset-password/{email}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> ResetPassword([FromRoute] string email)
        {
            Logger.Info($"Solicitud de restablecimiento de contraseña para: {email}");
            try
            {
                Logger.Debug("Obteniendo token de servicio para buscar usuario.");
                var token = await this._keycloakService.GetToken();

                Logger.Debug($"Buscando ID de Keycloak para {email}.");
                var id = await this._keycloakService.GetIdUsuarioKeycloakPorEmail(email, token);
                if (string.IsNullOrEmpty(id))
                {
                    Logger.Error($"Usuario no encontrado en Keycloak: {email}. Retornando BadRequest.");
                    return BadRequest("User not Found");
                }
                Logger.Info($"ID de Keycloak encontrado: {id} para {email}.");

                try
                {
                    var resetUrl = $"{Environment.GetEnvironmentVariable("KEYCLOAK_AUTHORITY_ADMIN")}/users/{id}/reset-password-email";
                    Logger.Info($"Intentando enviar correo de restablecimiento de Keycloak a {email} (URL: {resetUrl}).");

                    var emailRequest = new RestRequest(resetUrl, Method.Put);
                    emailRequest.AddHeader("Authorization", $"Bearer {token}");

                    var emailResponse = await _restClient.ExecuteAsync(emailRequest);

                    if (!emailResponse.IsSuccessful)
                    {
                        Logger.Error($"Keycloak falló al enviar el correo a {email}. Status: {emailResponse.StatusCode}," +
                                     $" Error: {emailResponse.ErrorMessage}");
                        return BadRequest($"Error al enviar correo: {emailResponse.ErrorMessage}");
                    }

                    await AuditoriaRepository.InsertarAuditoriaGateway(id, "INFO", "USUARIO_RESET_PSSWRD",
                        $"El usuario con ID '{id}' acaba de cambiar su contraseña ya que olvidó la original.");

                    Logger.Info($"Correo de restablecimiento enviado con éxito a {email}.");
                    return Ok("Correo Enviado!");
                }
                catch (Exception emailEx)
                {
                    Logger.Fatal($"Error fatal al ejecutar petición REST para enviar correo de restablecimiento a {email}.", emailEx);
                    return StatusCode(500, "Error interno al enviar el correo.");
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Error fatal (500) no controlado en el proceso de restablecimiento para {email}.", ex);
                return StatusCode(500, "Error interno en el servidor.");
            }
        }
        #endregion

        #region Update-Password (Cambiar contraseña)
        /// <summary>
        /// Permite a un usuario autenticado cambiar su propia contraseña.
        /// </summary>
        /// <param name="request">Contraseña nueva.</param>
        /// <returns>200 OK si la contraseña fue actualizada, o error 401/400/500.</returns>
        [HttpPost("update-password")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> UpdatePassword([FromBody] actualizarContrasenaDto request)
        {
            try
            {
                var email = GetAuthenticatedUserEmail();

                if (string.IsNullOrEmpty(email))
                {
                    Logger.Warn("Solicitud de cambio de contraseña sin email de usuario autenticado. Token inválido.");
                    return Unauthorized("No se pudo obtener el email del usuario.");
                }
                Logger.Info($"Iniciando cambio de contraseña para el usuario: {email}");

                var token = await this._keycloakService.GetToken();
                Logger.Debug("Token de servicio obtenido.");

                Logger.Debug($"Buscando ID de Keycloak para {email}.");
                var id = await this._keycloakService.GetIdUsuarioKeycloakPorEmail(email, token);

                if (string.IsNullOrEmpty(id))
                {
                    Logger.Error($"Usuario no encontrado en Keycloak: {email}. Retornando BadRequest.");
                    return BadRequest("User not found.");
                }
                Logger.Debug($"ID de Keycloak: {id}.");

                Logger.Info($"Actualizando contraseña en Keycloak para {email} (ID: {id}).");
                if (!await this._keycloakService.ActualizarContrasenaEnKeycloak(id, request.Contrasena, token))
                {
                    Logger.Error($"Keycloak reportó error al actualizar la contraseña para {email}.");
                    return BadRequest("Error al actualizar la contraseña en Keycloak.");
                }
                Logger.Info("Contraseña actualizada exitosamente en Keycloak.");

                await this._sendEmailService.SendCorreoActualizacionContrasenaConfirmacion(email);

                //Conexion con el MS de Usuarios para la publicacion de la actividad
                Logger.Debug($"[MS-USUARIOS] Preparando publicación de actividad para usuario: {id}.");
                var requestUsuario = new RestRequest(Environment.GetEnvironmentVariable("USUARIOS_MS_URL") +
                                                     $"/publishActivity", Method.Post);
                var activityBody = new
                    { idUsuario = id, accion = $"Actualización de Contraseña." };
                requestUsuario.AddJsonBody(activityBody);

                Logger.Debug($"[MS-USUARIOS] Enviando POST a {requestUsuario}. Payload: {{ userId: {id}, activityType: 'UserLogIn' }}");
                var responseUsuario = await _restClient.ExecuteAsync(requestUsuario);
                if (!responseUsuario.IsSuccessful)
                {
                    Logger.Error($"[500] ERROR DE COMUNICACIÓN EXTERNA [MS-USUARIOS]: El registro del Cambio de contraseña del usuario {id} fue exitoso, " +
                                 $"pero falló la publicación de la actividad del usuario {id}." +
                                 $" StatusCode: {responseUsuario.StatusCode}, Content: {responseUsuario.Content}",
                        responseUsuario.ErrorException);
                    return StatusCode(500, "Error al completar la publicación de la actividad del usuario");
                }
                Logger.Info($"[MS-USUARIOS] Actividad de usuario {id} publicada exitosamente. " +
                            $"Status: {responseUsuario.StatusCode}.");


                await AuditoriaRepository.InsertarAuditoriaGateway(id, "INFO", "USUARIO_CAMBIAR_PSSWRD",
                    $"El usuario con ID '{id}' acaba de cambiar su contraseña.");

                Logger.Info($"Proceso de cambio de contraseña completado exitosamente para {email}.");
                return Ok("Contraseña actualizada correctamente.");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Error fatal (500) no controlado durante el cambio de contraseña.", ex);
                return StatusCode(500, "Error interno en el servidor.");
            }
        }
        #endregion


        #region GetAuthenticatedUserEmail()
        //Saca la info del header del usuario autenticado
        private string GetAuthenticatedUserEmail()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            return email;
        }
        #endregion
    }
}
