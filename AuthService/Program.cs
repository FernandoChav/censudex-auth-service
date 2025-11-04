using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Clients;
namespace AuthService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --- 1. Registrar Servicios ---

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // A. Registrar el Cliente gRPC para hablar con ClientsService
        builder.Services.AddGrpcClient<Clients.ClientsClient>(o =>
        {
            // Lee la URL del appsettings.json
            var serviceUrl = builder.Configuration["ServiceUrls:ClientsService"];
            o.Address = new Uri(serviceUrl!);
        })
        // A.1 (IMPORTANTE) Para confiar en el certificado de desarrollo de tu ClientsService
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            // Esto permite la conexión a tu ClientsService local (que usa un certificado no confiable)
            handler.ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        // B. Registrar Redis para la Blocklist (cumple requisito de la rúbrica)
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var connectionString = builder.Configuration["Redis:ConnectionString"];
            return ConnectionMultiplexer.Connect(connectionString!);
        });

        // C. Registrar y Configurar la Autenticación JWT (cumple requisito de la rúbrica)
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
        });

        // D. Registrar el servicio de Autorización (para el requisito de Roles)
        builder.Services.AddAuthorization();


        var app = builder.Build();

        // --- 2. Configurar el Pipeline de HTTP ---

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // ¡IMPORTANTE! Activar Autenticación y Autorización
        // Esto debe ir ANTES de app.MapControllers()
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
