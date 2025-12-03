using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace AuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserRolesController(UserManager<AuthUser> userManager, RoleManager<AuthRole> roleManager) : ControllerBase
    {
        private readonly UserManager<AuthUser> _userManager = userManager;
        private readonly RoleManager<AuthRole> _roleManager = roleManager;


    }
}
