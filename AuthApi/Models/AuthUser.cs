using Microsoft.AspNetCore.Identity;

namespace AuthApi.Models
{
    public class AuthUser : IdentityUser
    {
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string ExtensionNumber { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}