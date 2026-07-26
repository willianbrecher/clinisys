using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CliniSys.Infrastructure.Persistence.Seeds;

public class AdminUserSeeder(UserManager<ApplicationUser> userManager) : IDataSeeder
{
    public int Order => 1;

    public async Task SeedAsync()
    {
        const string adminEmail = "admin@clinisys.local";

        if (await userManager.FindByNameAsync(adminEmail) is not null)
            return;

        var admin = new ApplicationUser
        {
            Id       = Guid.NewGuid(),
            UserName = adminEmail,
            Email    = adminEmail,
            FullName = "System Administrator",
            Role     = Role.Admin
        };

        await userManager.CreateAsync(admin, "Admin@12345");
    }
}
