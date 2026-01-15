using Microsoft.AspNetCore.Identity;
using VolunteerPlatform.Web.Models;

namespace VolunteerPlatform.Web.Middleware
{
    public class AutoRoleMiddleware
    {
        private readonly RequestDelegate _next;

        public AutoRoleMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    if (!roles.Any())
                    {
                        // Убедимся, что роль User существует
                        if (!await roleManager.RoleExistsAsync("User"))
                        {
                            await roleManager.CreateAsync(new IdentityRole("User"));
                        }
                        // Назначаем роль User
                        await userManager.AddToRoleAsync(user, "User");
                    }
                }
            }

            await _next(context);
        }
    }
}