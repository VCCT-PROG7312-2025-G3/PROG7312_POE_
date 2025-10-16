namespace PROG7312_POE.ViewModels
{
    public class LoginVm
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = "Resident"; // "Admin" or "Resident"
        public string? ReturnUrl { get; set; }
    }
}
