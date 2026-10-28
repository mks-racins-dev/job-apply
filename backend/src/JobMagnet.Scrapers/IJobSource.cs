namespace JobMagnet.Scrapers;

public interface IJobSource
{
    string Name { get; }

    Task<IReadOnlyList<ScrapedJob>> FetchAsync(CancellationToken cancellationToken);
}
