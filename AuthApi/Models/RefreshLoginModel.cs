namespace AuthApi.Models
{
    public class RefreshLoginModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
