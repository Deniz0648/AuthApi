namespace AuthApi.DTOs
{
    public class UpdateProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string ExtensionNumber { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        // Profil fotoğrafı güncellemesi için dosya yükleme alanı
        public IFormFile? ProfilePicture { get; set; }
    }
}
