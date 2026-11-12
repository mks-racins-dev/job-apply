namespace JobMagnet.Web.Api;

public record JobFeed(DateTimeOffset? LastCheckedAt, DateTimeOffset? LastAddedAt, List<SourceFeed> Sources);
