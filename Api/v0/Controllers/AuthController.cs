namespace devanewbot.Api.v0.Controllers;

using System.Linq;
using System.Threading.Tasks;
using devanewbot.Api.v0.Models.Auth;
using devanewbot.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[Route("/api/v0/auth")]
public class AuthController(SignInManager<User> signInManager, UserManager<User> userManager) : ControllerBase
{
    protected SignInManager<User> SignInManager { get; } = signInManager;
    protected UserManager<User> UserManager { get; } = userManager;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await UserManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await SignInManager.PasswordSignInAsync(user, model.Password, isPersistent: true, lockoutOnFailure: true);
        return result.Succeeded ? await Me() : Unauthorized();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await SignInManager.SignOutAsync();
        return Ok();
    }

    [HttpPost("password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        var user = await UserManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await UserManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(error => error.Description) });
        }

        await SignInManager.RefreshSignInAsync(user);
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await UserManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            user.Email,
            Roles = await UserManager.GetRolesAsync(user)
        });
    }
}
