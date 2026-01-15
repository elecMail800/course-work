using Microsoft.AspNetCore.Identity;
using VolunteerPlatform.Web.Models;

namespace VolunteerPlatform.Web.Services
{
    public class AutoRoleService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AutoRoleService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task AssignRoleToUserAsync(ApplicationUser user)
        {
            // Убедимся, что роль User существует
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Назначаем роль User
            await _userManager.AddToRoleAsync(user, "User");
        }
    }
}