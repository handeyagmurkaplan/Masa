using System.ComponentModel.DataAnnotations;

namespace Masa.Api.Auth;

public sealed record RegisterRequest(
    [Required, MaxLength(100)] string FullName,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MinLength(6), MaxLength(100)] string Password);

public sealed record LoginRequest(
    [Required] string Email,
    [Required] string Password);

public sealed record AuthResponse(string Token, string FullName, string Email);
