using System.Data;
using Dapper;
using Masa.Api.Common;
using Masa.Api.Data;
using Microsoft.AspNetCore.Identity;

namespace Masa.Api.Auth;

public sealed class AuthService(SqlConnectionFactory db, JwtService jwt)
{
    private readonly PasswordHasher<User> _hasher = new();

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var fullName = request.FullName.Trim();
        // Düz şifre prosedüre gitmez; yalnız hash.
        var hash = _hasher.HashPassword(new User { Email = email }, request.Password);

        using var conn = db.Create();
        var userId = await conn.QuerySingleAsync<int>(
            "sp_User_Insert",
            new { FullName = fullName, Email = email, PasswordHash = hash },
            commandType: CommandType.StoredProcedure);

        return new AuthResponse(jwt.CreateToken(userId, email), fullName, email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);

        using var conn = db.Create();
        var user = await conn.QuerySingleOrDefaultAsync<User>(
            "sp_User_GetByEmail",
            new { Email = email },
            commandType: CommandType.StoredProcedure);

        // Kullanıcı yok ve şifre yanlış aynı cevabı alır.
        if (user is null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw ApiException.AuthInvalid();

        return new AuthResponse(jwt.CreateToken(user.Id, user.Email), user.FullName, user.Email);
    }
}
