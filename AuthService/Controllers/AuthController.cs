using Microsoft.AspNetCore.Mvc;
using Grpc.Core;
using AuthService.Models;
using Clients;
using Microsoft.AspNetCore.Authorization;
using AuthService.Services.Interfaces;
using StackExchange.Redis;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
namespace AuthService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController(
        Clients.Clients.ClientsClient clientsClient,
        ITokenService tokenService,
        IConnectionMultiplexer redisDatabase
        ) : ControllerBase
    {
        // El cliente gRPC (con el namespace correcto)
        private readonly Clients.Clients.ClientsClient _clientsClient = clientsClient;
        private readonly ITokenService _tokenService = tokenService;
        private readonly IDatabase _redisDatabase = redisDatabase.GetDatabase();

        // --- 1. ENDPOINT DE LOGIN (Rúbrica: JWT, BCrypt, Roles) ---
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            ClientAuthResponse user;

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
                return Unauthorized(new { message = "Invalid credentials" });
            }

            if (user.Status != "active")
            {
                return Unauthorized(new { message = "User account is inactive" });
            }


            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }


            var tokenString = _tokenService.GenerateJwtToken(user.Id, user.Role);

            return Ok(new TokenDto { Token = tokenString });
        }


        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> LogoutAsync()
        {

            var jti = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            var expClaim = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
            if (jti == null || expClaim == null)
            {
                return BadRequest(new { message = "Invalid token" });
            }
            var expTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim));
            var timeUntilExpiry = expTime - DateTimeOffset.UtcNow;
            await _redisDatabase.StringSetAsync(jti, "blocked", timeUntilExpiry);

            return Ok(new { message = "Logged out successfully" });
        }


        [HttpGet("validate-token")]
        [Authorize]
        public async Task<IActionResult> ValidateToken()
        {

            var jti = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            if (jti != null && await _redisDatabase.KeyExistsAsync(jti))
            {

                return Unauthorized(new { message = "Token has been revoked" });
            }

            // El token es válido y no está en la blacklist
            var claims = User.Claims
                .GroupBy(c => c.Type)
                .Select(g => g.First())
                .ToDictionary(c => c.Type, c => c.Value);

            return Ok(new { valid = true, claims });
        }
    }
}
