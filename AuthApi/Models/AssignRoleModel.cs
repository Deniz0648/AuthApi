namespace AuthApi.Models
{
    public class AssignRoleModel
    {
        public string UserName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = [];
    }
}