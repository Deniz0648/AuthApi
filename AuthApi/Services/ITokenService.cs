using AuthApi.Models;
using System.Security.Claims;

namespace AuthApi.Services
{
    public interface ITokenService
    {
        Task<string> GenerateAccessToken(AuthUser user);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
