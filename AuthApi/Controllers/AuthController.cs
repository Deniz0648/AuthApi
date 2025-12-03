using AuthApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AuthApi.Services;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(UserManager<AuthUser> userManager,
        RoleManager<AuthRole> roleManager,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService) : ControllerBase
    {
        private readonly UserManager<AuthUser> _userManager = userManager;
        private readonly RoleManager<AuthRole> _roleManager = roleManager;
        private readonly ITokenService _tokenService = tokenService;
        private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;



        // 9. Login metodu
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized("Geçersiz kullanıcı adı veya şifre");

            // 15. Token üretimi: Access ve Refresh Token
            var accessToken = await _tokenService.GenerateAccessToken(user);
            var (refreshToken, expiration) = await _refreshTokenService.GenerateRefreshToken(user);

            return Ok(new   
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiration = expiration
            });
        }

        // 10. RefreshLogin metodu
        [HttpPost("refresh-login")]
        public async Task<IActionResult> RefreshLogin([FromBody] RefreshLoginModel model)
        {
            // 16. İlk olarak Access Token geçerliliğini kontrol et
            var principal = _tokenService.GetPrincipalFromExpiredToken(model.AccessToken);
            if (principal == null)
                return BadRequest("Geçersiz Access Token");

            var userName = principal.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return BadRequest("Token içinde kullanıcı adı bulunamadı.");
            }
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
                return BadRequest("Kullanıcı bulunamadı");

            // Veritabanından refresh token kontrolü yapılmalı
            var validRefreshToken = await _refreshTokenService.ValidateRefreshToken(user, model.RefreshToken);
            if (!validRefreshToken)
                return BadRequest("Geçersiz Refresh Token");

            // Yeni tokenleri üret
            var newAccessToken = await _tokenService.GenerateAccessToken(user);
            var (newRefreshToken, newExpiration) = await _refreshTokenService.GenerateRefreshToken(user);

            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiration = newExpiration
            });
        }
    }
}
