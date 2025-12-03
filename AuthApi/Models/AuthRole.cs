using Microsoft.AspNetCore.Identity;

namespace AuthApi.Models
{
    public class AuthRole : IdentityRole
    {
        public string Description { get; set; } = string.Empty;

        // Varsayılan parametresiz constructor
        public AuthRole() : base() { }

        // Rol adı ile oluşturmak için constructor
        public AuthRole(string roleName) : base(roleName) { }
    }
}
