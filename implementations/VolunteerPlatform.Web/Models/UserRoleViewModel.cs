namespace VolunteerPlatform.Web.Models
{
    public class UserRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<string> CurrentRoles { get; set; } = new();
        public List<string> AllRoles { get; set; } = new();
    }
}