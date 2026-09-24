namespace devanewbot.Api.v0.Models.Auth;

public class LoginModel
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
