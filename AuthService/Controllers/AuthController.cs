using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Threading.Tasks;
using Grpc.Core;
using AuthService.Models;
using Clients;
namespace AuthService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController(IConfiguration config, object clientsClient) : ControllerBase
    {
        private readonly IConfiguration _config = config;
        private readonly dynamic _clientsClient = clientsClient;

        // --- 1. ENDPOINT DE LOGIN (Cumple JWT y BCrypt) ---
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            ClientAuthResponse user;

            // 1. Llamar al ClientsService (vía gRPC)
            try
            {
                var request = new GetClientForAuthRequest
                {
                    EmailOrUsername = loginDto.EmailOrUsername
                };
                user = await _clientsClient.GetClientForAuthAsync(request);
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                // Usuario no encontrado en la BD del ClientsService
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // 2. Verificar si el usuario está activo
            if (user.Status != "active")
            {
                return Unauthorized(new { message = "User account is inactive" });
            }

            // 3. Verificar contraseña (Cumple requisito BCrypt)
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // 4. Generar Token (Cumple requisito JWT y Roles)
            var tokenString = GenerateJwtToken(user.Id, user.Role);

            return Ok(new TokenDto { Token = tokenString });
        }

        private string GenerateJwtToken(string userId, string userRole)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(
                securityKey, SecurityAlgorithms.HmacSha256);

            // Crear los "Claims" (Datos dentro del token)
            var claims = new[]
            {
                // El ID del usuario
                new Claim(JwtRegisteredClaimNames.Sub, userId), 
                // El Rol (para Autorización)
                new Claim(ClaimTypes.Role, userRole), 
                // El ID ÚNICO del token (para la Blacklist)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Crear el Token
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // Expiración de 1 hora
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
