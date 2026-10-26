namespace Masa.Api.Auth;

/// <summary>sp_User_GetByEmail satırı. PasswordHash asla JSON'a çıkmaz.</summary>
public sealed class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}
