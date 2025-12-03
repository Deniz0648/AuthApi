using Microsoft.AspNetCore.Identity;
using AuthApi.Models;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace AuthApi.Services
{
    public class RefreshTokenService(UserManager<AuthUser> userManager, IConfiguration configuration) : IRefreshTokenService
    {
        private readonly UserManager<AuthUser> _userManager = userManager;
        private readonly IConfiguration _configuration = configuration;

        public async Task<(string Token, DateTime Expiration)> GenerateRefreshToken(AuthUser user)
        {
            var refreshToken = Guid.NewGuid().ToString();
            var expiration = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["Jwt:RefreshTokenExpiration"]));

            // Mevcut token'ı sil
            await _userManager.RemoveAuthenticationTokenAsync(user, "AuthApi", "RefreshToken");
            await _userManager.RemoveAuthenticationTokenAsync(user, "AuthApi", "RefreshTokenExpiration");

            // Yeni token'ı ekle
            await _userManager.SetAuthenticationTokenAsync(user, "AuthApi", "RefreshToken", refreshToken);
            await _userManager.SetAuthenticationTokenAsync(user, "AuthApi", "RefreshTokenExpiration", expiration.ToString("o")); // ISO 8601 format

            return (refreshToken, expiration);
        }

        public async Task<bool> ValidateRefreshToken(AuthUser user, string refreshToken)
        {
            var storedToken = await _userManager.GetAuthenticationTokenAsync(user, "AuthApi", "RefreshToken");
            var expirationString = await _userManager.GetAuthenticationTokenAsync(user, "AuthApi", "RefreshTokenExpiration");

            // Temel kontrol
            if (string.IsNullOrEmpty(storedToken) ||
                string.IsNullOrEmpty(expirationString) ||
                storedToken != refreshToken)
            {
                return false;
            }

            // Expiration kontrolü
            if (!DateTime.TryParseExact(expirationString, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expirationDate))
            {
                return false;
            }

            return expirationDate > DateTime.UtcNow;
        }
    }
}
