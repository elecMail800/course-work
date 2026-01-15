using Microsoft.AspNetCore.Identity;

namespace VolunteerPlatform.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Добавляем поля для имени и фамилии
        [PersonalData]
        public string? FirstName { get; set; }

        [PersonalData]
        public string? LastName { get; set; }

        // Вычисляемое свойство для полного имени
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}