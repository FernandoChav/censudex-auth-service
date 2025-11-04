using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthService.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateJwtToken(string userId, string userRole);
    }
}