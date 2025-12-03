using System.ComponentModel.DataAnnotations;

namespace AuthApi.DTOs
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Yeni şifre gereklidir.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre onayı gereklidir.")]
        [Compare("NewPassword", ErrorMessage = "Yeni şifre ve onay şifresi uyuşmuyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
