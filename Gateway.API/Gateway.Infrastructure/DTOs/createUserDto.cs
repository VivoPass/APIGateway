
namespace Gateway.Infrastructure.DTOs
{
    public class createUserDto
    {
        public required string Nombre { get; init; }
        public required string Apellido { get; init; }
        public required string Correo { get; init; }
        public required string idRol { get; set; }
        public required string Contrasena { get; set; }

    }
}
