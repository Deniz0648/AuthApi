using AuthApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Contexts
{
    public class AutContexts(DbContextOptions<AutContexts> options) : IdentityDbContext<AuthUser, AuthRole, string,
        IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>,
        IdentityRoleClaim<string>, IdentityUserToken<string>>(options)
    {

        // Diğer DbSet'ler...
    }
}
