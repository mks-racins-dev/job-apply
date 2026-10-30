namespace JobMagnet.Web.Api;

public record MatchPage(
    List<MatchItem> Items, int Total, int NewCount, int InterestedCount, string Filter, FilterCounts Counts, int Page, int PageSize);
