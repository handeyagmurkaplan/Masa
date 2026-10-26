using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Masa.Api.Auth;

public sealed class JwtService(IConfiguration configuration)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

    public string CreateToken(int userId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var issuer = configuration["Jwt:Issuer"] ?? "Masa";
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: issuer,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
            ],
            expires: DateTime.UtcNow.Add(Lifetime),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
