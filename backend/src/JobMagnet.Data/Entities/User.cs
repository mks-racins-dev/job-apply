namespace JobMagnet.Data.Entities;

public class User
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public string? PasswordHash { get; set; }

    public string AuthProvider { get; set; } = "email";

    public string? PushToken { get; set; }

    // Index into the web app's pixel avatar catalog.
    public int AvatarId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<Match> Matches { get; set; } = [];
}
