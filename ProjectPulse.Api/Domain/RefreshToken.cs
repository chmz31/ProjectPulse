namespace ProjectPulse.Api.Domain;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // FK al usuario dueño del token
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    // El valor del token (cadena aleatoria, no JWT)
    public string Token { get; set; } = default!;

    // Gestión del ciclo de vida
    public DateTime ExpiresAt { get; set; }     // cuándo expira
    public DateTime? RevokedAt { get; set; }    // cuando ha sido invalidado (logout/rotación)
}
