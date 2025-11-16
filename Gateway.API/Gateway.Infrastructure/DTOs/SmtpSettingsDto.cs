
namespace Gateway.Infrastructure.DTOs
{
    public class SmtpSettingsDto
    {
        public string Host { get; set; } = "smtp.gmail.com"; // Default si no se sobreescribe
        public int Port { get; set; } = 587;
        public string Correo { get; set; } = "mariibarra14oct@gmail.com";
        public string Contrasena { get; set; } = "fsxg eetd ziwh anfz";
        public bool EnableSsl { get; set; } = true;
    }
}
