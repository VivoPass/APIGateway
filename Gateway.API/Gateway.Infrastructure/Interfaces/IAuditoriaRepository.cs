

namespace Gateway.Infrastructure.Interfaces
{
    public interface IAuditoriaRepository
    {
        Task InsertarAuditoriaGateway(string idUsuario, string level, string tipo, string mensaje);
    }
}
