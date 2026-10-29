using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using JobMagnet.Data;

namespace JobMagnet.Api.Auth;

public static class AccountEndpoints
{
    // Must stay in step with the number of avatars the clients can draw.
    public const int AvatarCount = 16;

    public record AccountResponse(string Email, int AvatarId);

    public record AvatarUpdate(int AvatarId);

    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/me").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapPut("/avatar", SetAvatarAsync);
    }

    private static async Task<IResult> GetAsync(ClaimsPrincipal principal, JobMagnetDbContext db)
    {
        var userId = principal.GetUserId();
        var account = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new AccountResponse(u.Email, u.AvatarId))
            .SingleOrDefaultAsync();

        return account is null ? Results.Unauthorized() : Results.Ok(account);
    }

    private static async Task<IResult> SetAvatarAsync(AvatarUpdate update, ClaimsPrincipal principal, JobMagnetDbContext db)
    {
        if (update.AvatarId is < 0 or >= AvatarCount)
        {
            return Results.BadRequest($"Avatar must be between 0 and {AvatarCount - 1}.");
        }

        var userId = principal.GetUserId();
        var updated = await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.AvatarId, update.AvatarId));

        return updated == 0 ? Results.Unauthorized() : Results.NoContent();
    }
}
