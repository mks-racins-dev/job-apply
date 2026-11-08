using Microsoft.EntityFrameworkCore;

using JobMagnet.Data;
using JobMagnet.Data.Entities;
using JobMagnet.Matching.Profiles;
using JobMagnet.Matching.Scoring;

namespace JobMagnet.Matching.Submitted;

// A job the user pasted in is extracted, scored and turned into a match right away, instead of waiting for the worker.
public class SubmittedJobService(
    JobMagnetDbContext db,
    JobPostingExtractor extractor,
    JobScorer scorer,
    TimeProvider timeProvider)
{
    public async Task<SubmitJobResult> SubmitAsync(Guid userId, string? url, string postingText, CancellationToken cancellationToken)
    {
        if (!SubmittedJobFactory.TryNormalizeUrl(url, out var normalizedUrl))
        {
            return new SubmitJobResult(SubmitJobStatus.InvalidUrl);
        }

        var profile = await db.Profiles.AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == userId && p.IsConfirmed, cancellationToken)
            .ConfigureAwait(false);
        if (profile is null || CandidateProfile.FromJson(profile.Data) is not { } candidate)
        {
            return new SubmitJobResult(SubmitJobStatus.ProfileNotConfirmed);
        }

        var externalId = SubmittedJobFactory.ComputeExternalId(userId, normalizedUrl, postingText);
        var existing = await db.Matches
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Job!.SubmittedByUserId == userId && m.Job.ExternalId == externalId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            // Pasting a job again is a clear sign of interest, so a dismissed one comes back.
            if (existing.Status == MatchStatus.Dismissed)
            {
                existing.Status = MatchStatus.Seen;
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return new SubmitJobResult(SubmitJobStatus.AlreadyAdded, existing.Id);
        }

        var extracted = await extractor.ExtractAsync(postingText, cancellationToken).ConfigureAwait(false);
        if (extracted is null)
        {
            return new SubmitJobResult(SubmitJobStatus.NotAJobPosting);
        }

        var now = timeProvider.GetUtcNow();
        var job = SubmittedJobFactory.Create(userId, normalizedUrl, postingText, extracted, now);
        job.Id = Guid.NewGuid();

        var score = (await scorer.ScoreAsync(candidate, [job], cancellationToken).ConfigureAwait(false)).FirstOrDefault();
        if (score is null)
        {
            return new SubmitJobResult(SubmitJobStatus.ScoringFailed);
        }

        var match = new Match
        {
            UserId = userId,
            Job = job,
            Score = score.Score,
            Reason = score.Reason,
            MatchedSkills = score.MatchedSkills,
            MissingSkills = score.MissingSkills,
            CreatedAt = now
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new SubmitJobResult(SubmitJobStatus.Created, match.Id);
    }
}
