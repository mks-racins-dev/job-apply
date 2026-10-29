using System.Security.Claims;

using JobMagnet.Api.Auth;
using JobMagnet.Api.Matches;
using JobMagnet.Data;
using JobMagnet.Matching.Submitted;

namespace JobMagnet.Api.Jobs;

public static class SubmittedJobEndpoints
{
    private const int MinPostingLength = 50;
    private const int MaxPostingLength = 30_000;

    public record SubmitJobRequest(string? Url, string Text);

    public static void MapSubmittedJobEndpoints(this IEndpointRouteBuilder app) =>
        app.MapPost("/jobs", SubmitAsync).RequireAuthorization();

    private static async Task<IResult> SubmitAsync(
        SubmitJobRequest request,
        ClaimsPrincipal principal,
        SubmittedJobService service,
        JobMagnetDbContext db,
        CancellationToken cancellationToken)
    {
        var text = request.Text?.Trim() ?? string.Empty;
        if (text.Length is < MinPostingLength or > MaxPostingLength)
        {
            return Results.BadRequest($"Paste the job posting text ({MinPostingLength}–{MaxPostingLength} characters).");
        }

        var userId = principal.GetUserId();
        var result = await service.SubmitAsync(userId, request.Url, text, cancellationToken);

        return result.Status switch
        {
            SubmitJobStatus.Created or SubmitJobStatus.AlreadyAdded =>
                Results.Ok(await MatchEndpoints.GetAsync(db, userId, result.MatchId!.Value)),
            SubmitJobStatus.InvalidUrl => Results.BadRequest("The link must be an http(s) URL."),
            SubmitJobStatus.ProfileNotConfirmed => Results.Conflict("Confirm your profile before adding jobs."),
            SubmitJobStatus.NotAJobPosting => Results.UnprocessableEntity("We couldn't find a job posting in that text."),
            SubmitJobStatus.ScoringFailed => Results.UnprocessableEntity("The job couldn't be scored. Try again."),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, "Unknown submit status.")
        };
    }
}
