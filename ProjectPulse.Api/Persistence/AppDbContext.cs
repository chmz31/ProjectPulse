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

        // Opcional: longitud razonable para el token (Base64 ~44 chars)
        b.Entity<RefreshToken>()
            .Property(rt => rt.Token)
            .HasMaxLength(100)
            .IsRequired();
    }
}
