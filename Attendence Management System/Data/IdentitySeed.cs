using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Data;

public static class IdentitySeed
{
    public const string AdminRole = "Admin";
    public const string TeacherRole = "Teacher";
    public const string StudentRole = "Student";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { AdminRole, TeacherRole, StudentRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // seeded single admin
        var adminEmail = config["SeedAdmin:Email"] ?? "admin@ams.local";
        var adminPassword = config["SeedAdmin:Password"] ?? "Admin@12345";
        var adminName = config["SeedAdmin:Name"] ?? "System Admin";

        var existingAdmins = await userManager.GetUsersInRoleAsync(AdminRole);
        if (existingAdmins.Count > 1)
        {
            // We keep the first admin; remove Admin role from others (do not delete accounts)
            foreach (var extraAdmin in existingAdmins.Skip(1))
                await userManager.RemoveFromRoleAsync(extraAdmin, AdminRole);
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                Name = adminName,
                EmailConfirmed = true
            };

            var create = await userManager.CreateAsync(admin, adminPassword);
            if (!create.Succeeded)
            {
                var msg = string.Join("; ", create.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create seeded admin: {msg}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
            await userManager.AddToRoleAsync(admin, AdminRole);
    }
}
