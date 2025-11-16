using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using log4net;

namespace Gateway.Infrastructure.Keycloak
{
    public static class ConfigureServicesExtension
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ConfigureServicesExtension));

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
        {
            _logger.Info("Iniciando configuración de autenticación con Keycloak.");

            try
            {
                var audience = Environment.GetEnvironmentVariable("KEYCLOAK_AUDIENCE");
                var authority = Environment.GetEnvironmentVariable("KEYCLOAK_AUTHORITY");

                if (string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(authority))
                {
                    _logger.Warn("Las variables de entorno KEYCLOAK_AUDIENCE o KEYCLOAK_AUTHORITY no están configuradas.");
                    throw new Exception("Error en configuración: las variables de entorno necesarias no están definidas.");
                }

                _logger.Info($"Configurando autenticación con Keycloak - Authority: {authority}, Audience: {audience}");

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = audience;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidIssuer = authority,
                        ClockSkew = TimeSpan.Zero,
                    };
                });

                _logger.Info("Autenticación con Keycloak configurada exitosamente.");
                return services;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error al configurar la autenticación con Keycloak: {ex.Message}", ex);
                throw;
            }
        }
    }
}
