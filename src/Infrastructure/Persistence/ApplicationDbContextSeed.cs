using Infrastructure.Identity.Entities;

namespace Infrastructure.Persistence;

public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken ct)
    {
        // Add your seeding logic here
        // Example:
        // if (!dbContext.Users.Any())
        // {
        //     dbContext.Users.Add(new User { ... });
        //     await dbContext.SaveChangesAsync(ct);
        // }
        if (!dbContext.Roles.Any())
        {
            dbContext.Roles.AddRange(
                new("Admin") { Description = "Administrator role with full permissions." },
                new("User") { Description = "Standard user role with limited permissions." }
            );
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
