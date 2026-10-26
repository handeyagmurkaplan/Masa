using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masa.Api.Auth;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var response = await auth.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    public Task<AuthResponse> Login(LoginRequest request) => auth.LoginAsync(request);
}
