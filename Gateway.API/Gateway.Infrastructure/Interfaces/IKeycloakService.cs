using Gateway.Infrastructure.DTOs;

namespace Gateway.Infrastructure.Interfaces
{
    public interface IKeycloakService
    {
        Task<bool> AsignarRolUsuario(string userId, string roleName, string token);
        Task<string> GetIdUsuarioKeycloakPorEmail(string correo, string token);
        Task<string> GetToken();
        Task<bool> SendCorreoVerificacion(string correo, string token);
        Task<string> RegistrarUsuarioEnKeycloak(createUserDto request, string token);
        Task<(string accessToken, string refreshToken, int expiresIn, string email, string idUsuario, string idRol)> AutenticarConKeycloak(string correo, string contrasena);
        Task<bool> ActualizarContrasenaEnKeycloak(string idUsuario, string contrasena, string token);
    }
}
