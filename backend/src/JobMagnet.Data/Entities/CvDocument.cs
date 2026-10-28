namespace JobMagnet.Data.Entities;

public class CvDocument
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public required string FileName { get; set; }

    public required string StoragePath { get; set; }

    public required string ExtractedText { get; set; }

    public DateTimeOffset UploadedAt { get; set; }
}
