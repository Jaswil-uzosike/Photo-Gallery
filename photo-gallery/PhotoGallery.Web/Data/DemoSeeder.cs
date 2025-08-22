    // What: Minimal demo seeder to ensure the UI isn't empty on first run. It Only runs in Development.
using Microsoft.AspNetCore.Identity;

namespace PhotoGallery.Web.Data
{
    public static class DemoSeeder
    {
        public static async Task RunAsync(IServiceProvider root)
        {
            using var scope = root.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // Seed one confirmed user for quick sign-in.
            var email = "demo@example.com";
            if (await users.FindByEmailAsync(email) is null)
            {
                var u = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                await users.CreateAsync(u, "Pass123$!"); // dev-only password
            }

            // OPTIONAL: If you have Gallery/Photo entities, you can seed them here.
            // Kept out to avoid compile errors if your schema differs.
        }
    }
}
