using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SecureController : ControllerBase
    {
        // Bu endpoint, [Authorize] attribute sayesinde yalnızca geçerli access token'a sahip kullanıcılar tarafından erişilebilir.
        [HttpGet("data")]
        [Authorize]
        public IActionResult GetSecureData()
        {
            //var controllerName = ControllerContext.ActionDescriptor.ControllerName;
            //var actionName = ControllerContext.ActionDescriptor.ActionName;

            // Kullanıcı bilgilerini token içerisinden al
            var userName = User.Identity?.Name;

            // ApiResponse<T> formatında dönen cevap
            var response = new ApiResponse<object>
            {
                StatusCode = 200, // Başarılı istek
                Message = "Doğrulama Başarıyla Yapıldı.",
                Data = new 
                {
                    User = userName,
                    Message = "Bu endpoint, geçerli access token ile korunmaktadır."
                },
                Errors = null
            };

            // API yanıtını döndür
            return Ok(response);
        }

    }
}