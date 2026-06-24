namespace ProjectPulse.Api.Domain;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = default!;
}
