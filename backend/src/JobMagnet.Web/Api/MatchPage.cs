namespace JobMagnet.Web.Api;

public record MatchPage(List<MatchItem> Items, int Total, int NewCount, int Page, int PageSize);
