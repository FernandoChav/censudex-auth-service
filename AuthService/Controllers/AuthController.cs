using Microsoft.AspNetCore.Mvc;
using Grpc.Core;
using AuthService.Models;
using Clients; // El namespace gRPC (corregido)
using Microsoft.AspNetCore.Authorization;
using AuthService.Services.Interfaces; // Para el [Authorize]
namespace AuthService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController(
        Clients.Clients.ClientsClient clientsClient,
        ITokenService tokenService) : ControllerBase
    {
        // El cliente gRPC (con el namespace correcto)
        private readonly Clients.Clients.ClientsClient _clientsClient = clientsClient;
        private readonly ITokenService _tokenService = tokenService;

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
        public IActionResult Logout()
        {
            
            return Ok(new { message = "Logged out successfully (Blacklist pending)" });
        }


        [HttpGet("validate-token")]
        [Authorize] 
        public IActionResult ValidateToken()
        {
            
            return Ok(new { valid = true });
        }
    }
}
