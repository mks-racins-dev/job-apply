using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using JobMagnet.Data.Entities;

namespace JobMagnet.Api.Auth;

public class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    public static SymmetricSecurityKey CreateKey(string signingKey) => new(Encoding.UTF8.GetBytes(signingKey));

    public string CreateToken(User user)
    {
        var jwt = options.Value;
        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Issuer,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Email, user.Email)],
            expires: timeProvider.GetUtcNow().Add(jwt.Lifetime).UtcDateTime,
            signingCredentials: new SigningCredentials(CreateKey(jwt.SigningKey), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
