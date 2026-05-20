

namespace Infrastructure.Seed
{
    // Seeds a demo admin user + role for development/demo purposes.
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            var adminRole = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = adminRole, NormalizedName = adminRole.ToUpperInvariant() });
            }

            var adminUserName = "admin";
            var admin = await userManager.FindByNameAsync(adminUserName);
            if (admin == null)
            {
                admin = new ApplicationUser { UserName = adminUserName, Email = "admin@example.com" };
                var res = await userManager.CreateAsync(admin, "Admin123!");
                if (res.Succeeded) await userManager.AddToRoleAsync(admin, adminRole);
            }
        }
    }
}
