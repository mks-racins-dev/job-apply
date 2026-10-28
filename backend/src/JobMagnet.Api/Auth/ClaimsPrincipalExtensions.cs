using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JobMagnet.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value ?? throw new InvalidOperationException("Authenticated user has no subject claim."));
    }
}
