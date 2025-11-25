using DotNetEnv;
using Gateway.API.Controllers;
using Gateway.Infrastructure.Configurations;
using Gateway.Infrastructure.Interfaces;
using Gateway.Infrastructure.Keycloak;
using Gateway.Infrastructure.Persistences.Repositories.MongoDB;
using Gateway.Infrastructure.Services;
using log4net;
using log4net.Config;
using RestSharp;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:44335");

// Configurar log4net
XmlConfigurator.Configure(new FileInfo("log4net.config"));
builder.Services.AddSingleton<ILog>(provider => LogManager.GetLogger(typeof(GatewayController)));

Env.Load();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IRestClient>(new RestClient());
builder.Services.AddSingleton<AuditoriaDbConfig>();
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));


builder.Services.AddJwtAuthentication();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy =>

        policy.RequireAuthenticatedUser()
    );
});

builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISmtpEmailSender, SmtpEmailSenderService>();
builder.Services.AddScoped<ISendEmailService, SendEmailService>();
builder.Services.AddScoped<IKeycloakService, KeycloakService>();


// Configuración CORS permisiva (¡Solo para desarrollo!)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // Permite cualquier dominio
            .AllowAnyMethod()  // GET, POST, PUT, DELETE, etc.
            .AllowAnyHeader(); // Cualquier cabecera
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
    c.RoutePrefix = "swagger"; // 🔹 Acceso en "/swagger"
});

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapReverseProxy();

app.Run();
