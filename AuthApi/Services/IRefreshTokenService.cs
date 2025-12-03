using AuthApi.Models;

namespace AuthApi.Services
{
    public interface IRefreshTokenService
    {
        Task<(string Token, DateTime Expiration)> GenerateRefreshToken(AuthUser user);
        Task<bool> ValidateRefreshToken(AuthUser user, string refreshToken);
    }
}