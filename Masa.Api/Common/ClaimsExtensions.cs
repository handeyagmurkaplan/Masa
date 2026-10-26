using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Masa.Api.Common;

public static class ClaimsExtensions
{
    /// <summary>UserId yalnız token'daki sub claim'inden okunur; gövdeden asla.</summary>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(sub, out var id) ? id : throw new ApiException(401, ErrorCodes.AuthInvalid, "Oturum geçersiz.");
    }
}
