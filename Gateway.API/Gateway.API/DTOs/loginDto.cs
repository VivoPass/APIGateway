namespace Gateway.API.DTOs
{
    public class loginDto
    {
        public required string Correo { get; init; }
        public required string Contrasena { get; set; }
    }
}
