using AuthApi.DTOs;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AuthApi.Helpers;

namespace AuthApi.Controllers
{
    // Kullanıcıların profilleriyle ilgili işlemleri yöneten controller
    [Authorize] // Yalnızca doğrulanmış kullanıcılar erişebilir
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController(UserManager<AuthUser> userManager) : ControllerBase
    {
        private readonly UserManager<AuthUser> _userManager = userManager;

        // 1. Kullanıcıya ait profil bilgilerini getiren metod
        [HttpGet("my-profile")]
        [Authorize] // Yalnızca doğrulama yapılmış kullanıcılar erişebilir
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Kullanıcı kimliği bulunamadıysa, yetkisiz erişim hatası döndür
            if (userId == null)
                return Unauthorized(new ApiResponse<object>
                {
                    StatusCode = 401,
                    Message = "Yetkisiz erişim.",
                    Data = null,
                    Errors = ["Kullanıcı kimliği bulunamadı."]
                });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    StatusCode = 404,
                    Message = "Kullanıcı bulunamadı.",
                    Data = null,
                    Errors = ["Kullanıcı bulunamadı."]
                });
            }

            // Kullanıcı bilgilerini DTO'ya map et (şifre gibi hassas veriler hariç)
            var profileDto = new ProfileDto
            {
                UserName = user.UserName?? "",
                ProfilePictureUrl = user.ProfilePictureUrl,
                FullName = user.FullName,
                EmployeeNumber = user.EmployeeNumber,
                ExtensionNumber = user.ExtensionNumber,
                Email = user.Email ?? string.Empty,
                Location = user.Location,
                Unit = user.Unit,
                Title = user.Title
            };

            return Ok(new ApiResponse<ProfileDto>
            {
                StatusCode = 200,
                Message = "Profil bilgileri başarıyla alındı.",
                Data = profileDto,
                Errors = null
            });
        }

        // 3. Kullanıcı profili güncelleme (Kullanıcıya ait)
        [HttpPut("my-profile")]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<IActionResult> UpdateMyProfile([FromForm] UpdateProfileDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new ApiResponse<object>
                {
                    StatusCode = 401,
                    Message = "Yetkilendirme başarısız.",
                    Data = null
                });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new ApiResponse<object>
                {
                    StatusCode = 404,
                    Message = "Kullanıcı bulunamadı.",
                    Data = null
                });

            // Önceki employee ID ve profil resmi URL'sini sakla
            string oldEmployeeNumber = user.EmployeeNumber;
            string oldProfilePictureUrl = user.ProfilePictureUrl;

            // Kullanıcıyı güncelle
            UserProfileHelper.UpdateUserFields(user, dto);

            // Profil resmi yönetimi
            try
            {
                if (dto.ProfilePicture != null)
                {
                    // Resim gönderildi, işle
                    string uploadResult = await UserProfileHelper.HandleProfilePictureAsync(dto.ProfilePicture,
                                                                                            oldEmployeeNumber,
                                                                                            user.EmployeeNumber,
                                                                                            oldProfilePictureUrl);

                    user.ProfilePictureUrl = $"{Request.Scheme}://{Request.Host}/{uploadResult}";
                }
                else if (oldEmployeeNumber != user.EmployeeNumber && !string.IsNullOrEmpty(oldProfilePictureUrl))
                {
                    // Sadece employee ID değişti ve mevcut resim var, klasör yapısını güncelle
                    string uploadResult = UserProfileHelper.UpdateProfilePictureLocation(oldEmployeeNumber,
                                                                                         user.EmployeeNumber,
                                                                                         oldProfilePictureUrl);

                    user.ProfilePictureUrl = $"{Request.Scheme}://{Request.Host}/{uploadResult}";
                }
                // Eğer resim gönderilmediyse, mevcut resim ve URL korunur
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    StatusCode = 400,
                    Message = "Profil güncellemesi başarısız oldu",
                    Errors = [ex.Message]
                });
            }

            // Kullanıcıyı veritabanına kaydet
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse<object>
                {
                    StatusCode = 400,
                    Message = "Profil güncellenirken bir hata oluştu.",
                    Errors = result.Errors.Select(e => e.Description),
                    Data = null
                });
            }

            return Ok(new ApiResponse<ProfileDto>
            {
                StatusCode = 200,
                Message = "Profil başarıyla güncellendi.",
                Data = new ProfileDto
                {
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    FullName = user.FullName,
                    EmployeeNumber = user.EmployeeNumber,
                    ExtensionNumber = user.ExtensionNumber,
                    Location = user.Location,
                    Unit = user.Unit,
                    Title = user.Title
                }
            });
        }

        [HttpPost("change-password")]
        [Authorize] // Yalnızca doğrulanmış kullanıcılar erişebilir
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    StatusCode = 400,
                    Message = "Geçersiz giriş.",
                    Errors = [.. ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)]
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    StatusCode = 401,
                    Message = "Yetkisiz erişim.",
                    Errors = ["Kullanıcı doğrulanamadı."]
                });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    StatusCode = 404,
                    Message = "Kullanıcı bulunamadı."
                });
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return BadRequest(new ApiResponse<object>
                {
                    StatusCode = 400,
                    Message = "Yeni şifre ve onay şifresi uyuşmuyor."
                });
            }

            try
            {
                var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        StatusCode = 400,
                        Message = "Şifre değiştirilemedi.",
                        Errors = [.. result.Errors.Select(e => e.Description)]
                    });
                }

                return NoContent(); // 204 No Content
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    StatusCode = 500,
                    Message = "Sunucu hatası oluştu.",
                    Errors = [ex.Message]
                });
            }
        }

    }
}
