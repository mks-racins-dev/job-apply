namespace JobMagnet.Web.Api;

public record ProfileResponse(ProfileData Data, int Version, bool IsConfirmed, DateTimeOffset UpdatedAt);
