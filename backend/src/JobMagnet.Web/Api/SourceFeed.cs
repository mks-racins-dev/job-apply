namespace JobMagnet.Web.Api;

public record SourceFeed(string Source, DateTimeOffset? LastAddedAt, int AddedLastDay, int Active);
