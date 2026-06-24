using Microsoft.EntityFrameworkCore;
using ProjectPulse.Api.Domain;

namespace ProjectPulse.Api.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

  // Tablas
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Project
        b.Entity<Project>().HasKey(p => p.Id);
        b.Entity<Project>()
            .HasIndex(p => p.OwnerId);
        b.Entity<Project>()
            .HasOne(p => p.Owner)
            .WithMany(u => u.Projects)
            .HasForeignKey(p => p.OwnerId);

        // User
        b.Entity<User>()
            .HasIndex(u => u.Email).IsUnique(); // email único
        b.Entity<User>()
            .Property(u => u.Email).IsRequired();
        b.Entity<User>()
            .Property(u => u.PasswordHash).IsRequired();
        b.Entity<User>()
            .Property(u => u.DisplayName).IsRequired();

        // RefreshToken (1:N con User)
        b.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId);

        b.Entity<RefreshToken>()
            .HasIndex(rt => rt.TokenHash)
            .IsUnique();
        b.Entity<RefreshToken>()
            .Property(rt => rt.TokenHash)
            .HasMaxLength(64)
            .IsRequired();
    }
}
