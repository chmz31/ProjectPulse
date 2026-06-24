namespace ProjectPulse.Api.Domain;

// Rol global simple para empezar (luego podemos afinar por proyecto)
public enum GlobalRole { Member = 0, Admin = 1 }

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Identidad básica
    public string Email { get; set; } = default!;        // único (lo haremos unique en DB)
    public string PasswordHash { get; set; } = default!; // guardaremos hash PBKDF2, nunca la contraseña
    public string DisplayName { get; set; } = default!;

    // Autorización
    public GlobalRole Role { get; set; } = GlobalRole.Member;

    // Relación 1:N con RefreshTokens
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
