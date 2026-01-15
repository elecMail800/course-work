using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VolunteerPlatform.Web.Data;
using VolunteerPlatform.Web.Middleware;
using VolunteerPlatform.Web.Models;
using VolunteerPlatform.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавляем поддержку MVC
builder.Services.AddControllersWithViews();

// Регистрируем ApplicationDbContext с подключением к SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрируем Identity с ApplicationUser
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Настройки пароля
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI(); // добавляем стандартные UI страницы для логина/регистрации

// Добавляем авторизацию с политиками
builder.Services.AddAuthorization(options =>
{
    // Политика для Гостей: разрешает доступ неаутентифицированным пользователям
    options.AddPolicy("GuestPolicy", policy =>
        policy.RequireAssertion(context =>
            !context.User.Identity?.IsAuthenticated == true));

    // Политика для Пользователей: требует аутентификации и роли User или Admin
    options.AddPolicy("UserPolicy", policy =>
        policy.RequireAuthenticatedUser().RequireRole("User", "Admin"));

    // Политика для Администраторов: требует аутентификации и роли Admin
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireAuthenticatedUser().RequireRole("Admin"));

    // Глобальная политика по умолчанию: требовать аутентификацию для всего
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
// Регистрируем AutoRoleService
builder.Services.AddScoped<AutoRoleService>();
// Добавляем Razor Pages (нужно для Identity UI)
builder.Services.AddRazorPages();

var app = builder.Build();

// Создание ролей и тестовых пользователей при старте приложения
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Синхронный вызов
        CreateRolesAndAdminAsync(roleManager, userManager).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating roles and admin user.");
    }
}


async Task CreateRolesAndAdminAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
{
    string[] roles = new[] { "Guest", "User", "Admin" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Создаем админа
    string adminEmail = "admin@volunteer.com";
    string adminPassword = "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    // Создаем тестового пользователя
    string testUserEmail = "user@volunteer.com";
    string testUserPassword = "User123!";

    var testUser = await userManager.FindByEmailAsync(testUserEmail);
    if (testUser == null)
    {
        testUser = new ApplicationUser
        {
            UserName = testUserEmail,
            Email = testUserEmail,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(testUser, testUserPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(testUser, "User");
        }
    }

    // ДОБАВЬТЕ ЭТОТ БЛОК - назначаем роль User всем пользователям без роли
    var allUsers = userManager.Users.ToList();
    foreach (var user in allUsers)
    {
        var userRoles = await userManager.GetRolesAsync(user);
        if (!userRoles.Any())
        {
            await userManager.AddToRoleAsync(user, "User");
        }
    }
}



// Конвейер HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
// Добавляем middleware для автоматического назначения роли
app.UseMiddleware<AutoRoleMiddleware>();

// Явные маршруты для контроллеров
app.MapControllerRoute(
    name: "events",
    pattern: "Events/{action=Index}/{id?}",
    defaults: new { controller = "Events" });

app.MapControllerRoute(
    name: "users",
    pattern: "Users/{action=Index}/{id?}",
    defaults: new { controller = "Users" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Подключаем Razor Pages (Identity UI)
app.MapRazorPages();

app.Run();