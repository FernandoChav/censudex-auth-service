using System.ComponentModel.DataAnnotations;
namespace AuthService.Models
{
    public class LoginDto
    {
        [Required]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
    public class TokenDto
    {
        public string Token { get; set; } = string.Empty;
    }
}