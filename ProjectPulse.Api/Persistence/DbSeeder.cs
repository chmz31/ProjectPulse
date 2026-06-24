using Microsoft.EntityFrameworkCore;
using ProjectPulse.Api.Domain;
using ProjectPulse.Api.Services;

namespace ProjectPulse.Api.Persistence;

public static class DbSeeder
{
    public static async Task EnsureSeededAsync(this AppDbContext db, IPasswordHasher hasher, ILogger logger)
    {
        // migrate en arranque (dev)
        await db.Database.MigrateAsync();

        var doSeed = Environment.GetEnvironmentVariable("SEED")?.ToLowerInvariant() == "true";
        if (!doSeed) return;

        var adminPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("SEED is enabled, but SEED_ADMIN_PASSWORD is not set. Skipping seed data.");
            return;
        }

        if (!await db.Users.AnyAsync())
        {
            var admin = new User
            {
                Email = "admin@pulse.dev",
                DisplayName = "Admin",
                PasswordHash = hasher.Hash(adminPassword),
                Role = GlobalRole.Admin
            };
            db.Users.Add(admin);

            db.Projects.AddRange(
                new Project { Name = "ProjectPulse", Description = "Demo seed", OwnerId = admin.Id },
                new Project { Name = "Onboarding", Description = "Sample project", OwnerId = admin.Id }
            );

            await db.SaveChangesAsync();
            logger.LogInformation("Seeded admin user and sample projects.");
        }
    }
}
