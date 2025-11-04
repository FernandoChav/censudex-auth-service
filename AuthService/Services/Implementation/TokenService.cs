using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AuthService.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services.Implementation
{
    public class TokenService(IConfiguration config) : ITokenService
    {
        private readonly IConfiguration _config = config;

        public string GenerateJwtToken(string userId, string userRole)
        {
            var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(
                securityKey, SecurityAlgorithms.HmacSha256);

            // Crear los "Claims" (Datos dentro del token)
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, userId), // El ID del usuario
            new Claim(ClaimTypes.Role, userRole), // El Rol (para Autorización)
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // El ID ÚNICO del token
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