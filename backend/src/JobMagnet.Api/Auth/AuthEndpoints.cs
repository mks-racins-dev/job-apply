using JobMagnet.Data;
using JobMagnet.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JobMagnet.Api.Auth;

public static class AuthEndpoints
{
    private const int MinPasswordLength = 8;

    public record Credentials(string Email, string Password);

    public record TokenResponse(string Token);

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
    }

    private static async Task<IResult> RegisterAsync(
        Credentials request, JobMagnetDbContext db, IPasswordHasher<User> hasher, TokenService tokens, TimeProvider timeProvider)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (!email.Contains('@') || request.Password.Length < MinPasswordLength)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["credentials"] = [$"A valid email and a password of at least {MinPasswordLength} characters are required."]
            });
        }

        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return Results.Conflict();
        }

        var user = new User { Email = email, CreatedAt = timeProvider.GetUtcNow() };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Results.Ok(new TokenResponse(tokens.CreateToken(user)));
    }

    private static async Task<IResult> LoginAsync(
        Credentials request, JobMagnetDbContext db, IPasswordHasher<User> hasher, TokenService tokens)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user?.PasswordHash is null
            || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new TokenResponse(tokens.CreateToken(user)));
    }
}
