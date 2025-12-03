using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AuthApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace AuthApi.Services;

public class TokenService(UserManager<AuthUser> userManager, IConfiguration configuration) : ITokenService
{
    private readonly UserManager<AuthUser> _userManager = userManager;
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Kullanıcının kimlik bilgileri ve rollerine göre JWT erişim token'ı oluşturur.
    /// </summary>
    /// <param name="user">Token oluşturulacak kullanıcı</param>
    /// <returns>Oluşturulan JWT token'ı string olarak döner</returns>
    public async Task<string> GenerateAccessToken(AuthUser user)
    {
        // Kullanıcının rollerini al
        var roles = await _userManager.GetRolesAsync(user);

        // Token için temel claim'leri oluştur
        var authClaims = new List<Claim>
{
    new(ClaimTypes.Name, user.UserName ?? ""),
    new(ClaimTypes.NameIdentifier, user.Id),
    new("FullName", user.FullName ?? ""),
    new("EmployeeNumber", user.EmployeeNumber ?? ""),
    new("Unit", user.Unit ?? ""),
    new("Title", user.Title ?? ""),
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
};

        // Her rol için ayrı ayrı claim ekleniyor
        foreach (var role in roles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Token geçerlilik süresi ayarının güvenli şekilde parse edilmesi
        if (!double.TryParse(_configuration["Jwt:TokenExpiration"], out double tokenExpirationMinutes))
        {
            throw new ArgumentException("Invalid token expiration setting.");
        }

        // Güvenli anahtar çekimi: Önce ortam değişkeninden, yoksa konfigürasyondan alınır
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT_SECRET_KEY bulunamadı veya yapılandırma dosyasında eksik.");
        }

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        // JWT token'ı oluştur
        var jwtToken = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: DateTime.UtcNow.AddMinutes(tokenExpirationMinutes),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        // Token'ı string formatına çevir
        var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        // Token'in AspNetUserTokens tablosuna kaydedilmesi
        // "JWT" login provider ve "AccessToken" token adı örnek olarak kullanılmıştır.
        // Bu değerleri uygulamanızın ihtiyaçlarına göre ayarlayabilirsiniz.
        await _userManager.SetAuthenticationTokenAsync(user, "JWT", "AccessToken", token);

        return token;
    }


    /// <summary>
    /// Geçmiş token'dan ClaimsPrincipal elde etmeye çalışır (örneğin, token yenileme senaryosunda).
    /// Token süresi dolmuş olsa bile doğrulama yapılır.
    /// </summary>
    /// <param name="token">Doğrulanacak token</param>
    /// <returns>Geçerli ClaimsPrincipal veya doğrulama başarısızsa null</returns>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentNullException(token, "token bulunamadı.");
        }
        // Güvenli anahtar çekimi: JWT_SECRET_KEY önce ortam değişkeninden, yoksa konfigürasyondan alınır
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? _configuration["Jwt:Key"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT_SECRET_KEY bulunamadı veya yapılandırma dosyasında eksik.");
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            // Token'ın süresi dolmuş olsa bile doğrulama yapılabilsin diye
            // lifetime kontrolü devre dışı bırakılır
            ValidateLifetime = false,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out var securityToken);
            // Token'ın beklenen algoritmayı kullandığından emin olun
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }
            return principal;
        }
        catch
        {
            // Hata durumunda ek loglama yapılabilir
            return null;
        }
    }
}
