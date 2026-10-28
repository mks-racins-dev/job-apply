namespace JobMagnet.Data.Entities;

public class Profile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    // Raw JSON so the profile schema can evolve with the prompt without migrations.
    public required string Data { get; set; }

    public int Version { get; set; }

    public bool IsConfirmed { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
