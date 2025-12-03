using AuthApi.Contexts;
using AuthApi.DTOs;
using AuthApi.Helpers;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthApi.Controllers
{
    //[Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController(UserManager<AuthUser> userManager, RoleManager<AuthRole> roleManager, AutContexts context) : ControllerBase
    {
        private readonly UserManager<AuthUser> _userManager = userManager;
        private readonly RoleManager<AuthRole> _roleManager = roleManager;
        private readonly AutContexts _context = context;

        // 8. Register metodu
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<string>>> Register([FromBody] RegisterModel model)
        {
            var apiResponse = new ApiResponse<string>();

            try
            {
                // Check if user already exists
                var userExist = await _userManager.FindByNameAsync(model.UserName);
                if (userExist != null)
                {
                    apiResponse.StatusCode = StatusCodes.Status400BadRequest;
                    apiResponse.Message = "Kayıt başarısız";
                    apiResponse.Errors = ["Kullanıcı zaten mevcut"];
                    return BadRequest(apiResponse);
                }

                // Create new user
                var user = new AuthUser
                {
                    ProfilePictureUrl = string.Empty,
                    UserName = model.UserName,
                    Email = model.Email,
                    FullName = string.Empty,
                    EmployeeNumber = string.Empty,
                    ExtensionNumber = string.Empty,
                    Location = string.Empty,
                    Unit = string.Empty,
                    Title = string.Empty
                };

                // Attempt to create user
                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    apiResponse.StatusCode = StatusCodes.Status400BadRequest;
                    apiResponse.Message = "Kayıt başarısız";
                    apiResponse.Errors = result.Errors.Select(e => e.Description);
                    return BadRequest(apiResponse);
                }

                // Ensure "User" role exists
                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    await _roleManager.CreateAsync(new AuthRole("User"));
                }

                // Assign user role
                await _userManager.AddToRoleAsync(user, "User");

                // Successful registration
                apiResponse.StatusCode = StatusCodes.Status201Created;
                apiResponse.Message = "Kayıt başarılı";
                apiResponse.Data = user.Id;

                return CreatedAtAction(nameof(Register), apiResponse);
            }
            catch (Exception ex)
            {
                apiResponse.StatusCode = StatusCodes.Status500InternalServerError;
                apiResponse.Message = "Bir hata oluştu";
                apiResponse.Errors = [ex.Message];
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }


        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var allPeople = new List<AllProfileDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                var people = new AllProfileDto
                {
                    UserName = user.UserName ?? string.Empty,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    FullName = user.FullName,
                    EmployeeNumber = user.EmployeeNumber,
                    ExtensionNumber = user.ExtensionNumber,
                    Email = user.Email ?? string.Empty,
                    Location = user.Location,
                    Unit = user.Unit,
                    Title = user.Title,
                    Roles = [.. roles] // Rolleri ekliyoruz
                };

                allPeople.Add(people);
            }

            return Ok(new ApiResponse<IEnumerable<AllProfileDto>>
            {
                StatusCode = 200,
                Message = "Kullanıcı profilleri ve rolleri başarıyla alındı",
                Data = allPeople,
                Errors = []
            });
        }

        [HttpPut("users/{username}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateUser(string username, [FromForm] UpdateProfileDto dto)
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

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    StatusCode = 404,
                    Message = "Kullanıcı bulunamadı.",
                    Errors = ["Hedef kullanıcı bulunamadı."]
                });
            }

            string oldEmployeeNumber = user.EmployeeNumber;
            string oldProfilePictureUrl = user.ProfilePictureUrl;

            UserProfileHelper.UpdateUserFields(user, dto);

            try
            {
                if (dto.ProfilePicture != null)
                {
                    string uploadResult = await UserProfileHelper.HandleProfilePictureAsync(dto.ProfilePicture,
                                                                                            oldEmployeeNumber,
                                                                                            user.EmployeeNumber,
                                                                                            oldProfilePictureUrl);
                    user.ProfilePictureUrl = $"{Request.Scheme}://{Request.Host}/{uploadResult}";
                }
                else if (oldEmployeeNumber != user.EmployeeNumber && !string.IsNullOrEmpty(oldProfilePictureUrl))
                {
                    string uploadResult = UserProfileHelper.UpdateProfilePictureLocation(oldEmployeeNumber,
                                                                                         user.EmployeeNumber,
                                                                                         oldProfilePictureUrl);
                    user.ProfilePictureUrl = $"{Request.Scheme}://{Request.Host}/{uploadResult}";
                }

                // Kullanıcıyı veritabanına kaydet
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        StatusCode = 400,
                        Message = "Profil güncellenirken bir hata oluştu.",
                        Errors = [.. result.Errors.Select(e => e.Description)]
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
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    StatusCode = 500,
                    Message = "Profil güncellenirken beklenmeyen bir hata oluştu.",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpPost("{username}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(string username, [FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object>
                {
                    StatusCode = 400,
                    Message = "Geçersiz giriş.",
                    Errors = [.. ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)]
                });

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound(new ApiResponse<object>
                {
                    StatusCode = 404,
                    Message = "Hedef kullanıcı bulunamadı."
                });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new ApiResponse<object>
                {
                    StatusCode = 400,
                    Message = "Yeni şifre ve onay şifresi uyuşmuyor."
                });

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

                if (!result.Succeeded)
                    return BadRequest(new ApiResponse<object>
                    {
                        StatusCode = 400,
                        Message = "Şifre sıfırlama başarısız oldu.",
                        Errors = [.. result.Errors.Select(e => e.Description)]
                    });

                return Ok(new ApiResponse<object>
                {
                    StatusCode = 200,
                    Message = "Şifre başarıyla sıfırlandı.",
                });
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

        [HttpDelete("{username}")]
        public async Task<IActionResult> DeleteUserAccount(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    StatusCode = 404,
                    Message = "Hedef kullanıcı bulunamadı."
                });
            }

            // 1️⃣ Kullanıcının rollerini temizle
            var existingRoles = await _userManager.GetRolesAsync(user);
            if (existingRoles.Any())
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, existingRoles);
                if (!removeRolesResult.Succeeded)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        StatusCode = 400,
                        Message = "Kullanıcı rolleri temizlenirken bir hata oluştu.",
                        Errors = removeRolesResult.Errors.Select(e => e.Description)
                    });
                }
            }

            // 2️⃣ Kullanıcının tokenlarını temizle
            var tokens = _context.UserTokens.Where(t => t.UserId == user.Id);
            _context.UserTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync(); // Tokenları kalıcı olarak sil

            // 3️⃣ Kullanıcıyı sil
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return BadRequest(new ApiResponse<object>
                {
                    StatusCode = 400,
                    Message = "Kullanıcı hesabı silinemedi.",
                    Errors = deleteResult.Errors.Select(e => e.Description)
                });
            }

            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                Message = "Kullanıcı ve ilişkili veriler başarıyla silindi."
            });
        }


        // 3. Tüm Roller
        [HttpGet("roles")]
        public IActionResult GetAllRoles()
        {
            var rolesList = _roleManager.Roles.ToList();

            var roles = rolesList.Select(r => new RoleDto
            {
                Name = r.Name ?? string.Empty,
                Description = (r is AuthRole authRole) ? authRole.Description : string.Empty
            }).ToList();

            return Ok(new ApiResponse<List<RoleDto>>
            {
                StatusCode = 200,
                Message = "Roller başarıyla getirildi.",
                Data = roles
            });
        }

        // 1. Yeni Rol Oluşturma
        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleModel model)
        {
            if (string.IsNullOrEmpty(model.RoleName))
            {
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    Message = "Rol adı boş olamaz."
                });
            }

            // Rol adını PascalCase formatına çevir
            var formattedRoleName = ToPascalCase(model.RoleName);

            var roleExist = await _roleManager.RoleExistsAsync(formattedRoleName);
            if (roleExist)
            {
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    Message = $"'{formattedRoleName}' adıyla bir rol zaten mevcut."
                });
            }

            var newRole = new AuthRole { Name = formattedRoleName, Description = model.Description };
            var result = await _roleManager.CreateAsync(newRole);
            if (result.Succeeded)
            {
                return Ok(new ApiResponse<string>
                {
                    StatusCode = 200,
                    Message = $"'{formattedRoleName}' rolü başarılı bir şekilde oluşturuldu."
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                StatusCode = 400,
                Message = "Rol oluşturulurken bir hata oluştu.",
                Errors = result.Errors.Select(e => e.Description)
            });
        }

        // Yardımcı metot: PascalCase dönüşümü
        private static string ToPascalCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return string.Join("", input
                .Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpper(word[0]) + word[1..].ToLower()));
        }

        // 1. Kullanıcıya Rol Atama
        [HttpPost("assign-roles")]
        public async Task<IActionResult> AssignRolesToUser([FromBody] AssignRoleModel model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    StatusCode = 404,
                    Message = $"Kullanıcı '{model.UserName}' bulunamadı."
                });
            }

            // Roller asenkron şekilde sıralı kontrol ediliyor
            foreach (var role in model.Roles)
            {
                var roleExists = await _roleManager.RoleExistsAsync(role);
                if (!roleExists)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        StatusCode = 400,
                        Message = $"Rol '{role}' bulunamadı."
                    });
                }
            }

            // Kullanıcının mevcut rollerini al
            var existingRoles = await _userManager.GetRolesAsync(user);

            // Eğer mevcut roller ile yeni roller aynıysa işlem yapmaya gerek yok
            if (existingRoles.OrderBy(r => r).SequenceEqual(model.Roles.OrderBy(r => r)))
            {
                return Ok(new ApiResponse<string>
                {
                    StatusCode = 200,
                    Message = "Kullanıcının mevcut rolleri ile istenen roller aynı. Güncelleme yapılmadı."
                });
            }

            // Silinmesi gereken roller (kullanıcıda olup yeni listede olmayanlar)
            var rolesToRemove = existingRoles.Except(model.Roles).ToList();

            // Eklenmesi gereken roller (yeni listede olup kullanıcıda olmayanlar)
            var rolesToAdd = model.Roles.Except(existingRoles).ToList();

            IdentityResult removeResult = IdentityResult.Success;
            if (rolesToRemove.Count != 0)
            {
                removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            if (!removeResult.Succeeded)
            {
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    Message = "Önceki roller kaldırılırken bir hata oluştu.",
                    Errors = removeResult.Errors.Select(e => e.Description)
                });
            }

            IdentityResult addResult = IdentityResult.Success;
            if (rolesToAdd.Count != 0)
            {
                addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            if (addResult.Succeeded)
            {
                string updateMessage = rolesToRemove.Count != 0 && rolesToAdd.Count != 0
                    ? $"Kullanıcı '{model.UserName}' için eski roller kaldırıldı ve yeni roller eklendi."
                    : rolesToRemove.Count != 0
                        ? $"Kullanıcı '{model.UserName}' bazı rollerden çıkarıldı: {string.Join(", ", rolesToRemove)}."
                        : $"Kullanıcı '{model.UserName}' yeni rollere eklendi: {string.Join(", ", rolesToAdd)}.";

                return Ok(new ApiResponse<string>
                {
                    StatusCode = 200,
                    Message = updateMessage
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                StatusCode = 400,
                Message = "Yeni roller eklenirken bir hata oluştu.",
                Errors = addResult.Errors.Select(e => e.Description)
            });
        }





        // 2. Kullanıcının Mevcut Rollerini Görüntüleme
        [HttpGet("user-roles/{userName}")]
        public async Task<IActionResult> GetUserRoles(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return NotFound(new ApiResponse<List<RoleDto>>
                {
                    StatusCode = 404,
                    Message = $"Kullanıcı '{userName}' bulunamadı."
                });
            }

            var roleNames = await _userManager.GetRolesAsync(user);

            // Rolleri önce listeye al (veritabanı sorgusunu bitir)
            var roleEntities = _roleManager.Roles
                .Where(r => roleNames.Contains(r.Name ?? string.Empty))
                .ToList(); // **Belleğe alındı!**

            // Sonrasında LINQ Select işlemi yap
            var roles = roleEntities.Select(r => new RoleDto
            {
                Name = r.Name ?? string.Empty,
                Description = (r is AuthRole authRole) ? authRole.Description : string.Empty
            })
            .ToList();

            return Ok(new ApiResponse<List<RoleDto>>
            {
                StatusCode = 200,
                Message = "Kullanıcı rolleri başarıyla getirildi.",
                Data = roles
            });
        }


        [HttpDelete("delete-roles/{roleName}")]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            // "Admin" rolü silinemez
            if (roleName == "Admin")
            {
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    Message = "Admin rolü silinemez."
                });
            }
            // Rolü bul
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    StatusCode = 404,
                    Message = $"Rol '{roleName}' bulunamadı."
                });
            }

            // Kullanıcıları bul ve bu rolden çıkar
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            foreach (var user in usersInRole)
            {
                await _userManager.RemoveFromRoleAsync(user, roleName);
            }

            // Rolü sil
            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse<string>
                {
                    StatusCode = 400,
                    Message = $"Rol '{roleName}' silinemedi.",
                    Errors = [.. result.Errors.Select(e => e.Description)]
                });
            }

            return Ok(new ApiResponse<string>
            {
                StatusCode = 200,
                Message = $"Rol '{roleName}' başarıyla silindi ve bu rol tüm kullanıcılardan kaldırıldı."
            });
        }



    }
}
