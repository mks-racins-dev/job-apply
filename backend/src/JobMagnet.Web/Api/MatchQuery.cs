namespace JobMagnet.Web.Api;

public record MatchQuery(int MinScore, int Page, int PageSize, string Sort, bool Descending, string Filter);
