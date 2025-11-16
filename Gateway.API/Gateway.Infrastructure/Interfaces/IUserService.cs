using Gateway.Infrastructure.DTOs;

namespace Gateway.Infrastructure.Interfaces
{
    public interface IUserService
    {
        Task SaveActividadUsuario(string idUsuario, string accion, string token);
        Task<(string idUsuario, string idRol)> GetUsuarioDelService(string correo, string accessToken);
        //Task<bool> RegistrarUsuarioEnBD(createUserDto request, string token);
        Task<string> GetIDUsuarioDeBD(string correo, string token);
    }
}
