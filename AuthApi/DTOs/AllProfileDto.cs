namespace AuthApi.DTOs
{
    public class AllProfileDto
    {
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string ExtensionNumber { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public object Email { get; set; } = string.Empty;

        public List<String> Roles { get; set; } = [];   
    }

}
