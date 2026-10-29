using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using JobMagnet.Data;
using JobMagnet.Data.Entities;
using JobMagnet.Matching.Scoring;

namespace JobMagnet.Matching;

public class MatchingService(
    JobMagnetDbContext db,
    JobScorer scorer,
    IOptions<MatchingOptions> options,
    TimeProvider timeProvider,
    ILogger<MatchingService> logger)
{
    public async Task<int> RunForAllUsersAsync(CancellationToken cancellationToken)
    {
        var profiles = await db.Profiles.AsNoTracking()
            .Where(p => p.IsConfirmed)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var total = 0;
        foreach (var profile in profiles)
        {
            total += await RunForProfileAsync(profile, cancellationToken).ConfigureAwait(false);
        }

        return total;
    }

    public async Task<int> RunForProfileAsync(Profile profile, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var alreadyMatched = db.Matches.Where(m => m.UserId == profile.UserId).Select(m => m.JobId);
        var unmatchedJobs = await db.Jobs.AsNoTracking()
            .Where(j => j.IsActive && j.SubmittedByUserId == null && !alreadyMatched.Contains(j.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var candidates = CandidatePrefilter.SelectCandidates(
            ProfileTerms.FromJson(profile.Data), unmatchedJobs, settings.MaxCandidatesPerUser);

        var strongMatches = 0;
        foreach (var batch in candidates.Chunk(settings.JobsPerScoringRequest))
        {
            var scores = await scorer.ScoreAsync(profile.Data, batch, cancellationToken).ConfigureAwait(false);
            db.Matches.AddRange(scores.Select(score => ToMatch(profile.UserId, score)));
            strongMatches += scores.Count(score => score.Score >= settings.NotifyThreshold);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "User {UserId}: scored {Candidates} candidates, {Strong} above threshold", profile.UserId, candidates.Count, strongMatches);
        return strongMatches;
    }

    // Low scores are stored too, so the same job isn't sent to Claude again on the next run.
    private Match ToMatch(Guid userId, JobScore score) => new()
    {
        UserId = userId,
        JobId = score.JobId,
        Score = score.Score,
        Reason = score.Reason,
        MatchedSkills = score.MatchedSkills,
        MissingSkills = score.MissingSkills,
        CreatedAt = timeProvider.GetUtcNow()
    };
}
