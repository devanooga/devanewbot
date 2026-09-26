namespace devanewbot.Api.v0.Controllers;

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using devanewbot.Api.v0.Models.Auth;
using devanewbot.Data.Models;
using devanewbot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[Route("/api/v0/auth")]
public class AuthController(
    SignInManager<User> signInManager,
    UserManager<User> userManager,
    SlackSignIn slackSignIn,
    SlackDirectory slackDirectory,
    IOptions<SiteOptions> siteOptions) : ControllerBase
{
    private const string SlackStateCookie = "slack-sign-in";

    protected SignInManager<User> SignInManager { get; } = signInManager;
    protected UserManager<User> UserManager { get; } = userManager;
    protected SlackSignIn SlackSignIn { get; } = slackSignIn;
    protected SlackDirectory SlackDirectory { get; } = slackDirectory;
    protected SiteOptions Site { get; } = siteOptions.Value;

    [HttpGet("providers")]
    [AllowAnonymous]
    public IActionResult Providers() => Ok(new { Slack = SlackSignIn.Configured });

    [HttpGet("slack/start")]
    [AllowAnonymous]
    public async Task<IActionResult> StartSlack([FromQuery] string mode = "login")
    {
        if (!SlackSignIn.Configured)
        {
            return NotFound();
        }

        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        Response.Cookies.Append(SlackStateCookie, $"{state}:{(mode == "link" ? "link" : "login")}", new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
        });

        return Redirect(await SlackSignIn.AuthorizeUrl(SlackRedirectUri(), state));
    }

    [HttpGet("slack/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> SlackCallback([FromQuery] string? code, [FromQuery] string? state)
    {
        var expected = Request.Cookies[SlackStateCookie]?.Split(':');
        Response.Cookies.Delete(SlackStateCookie);
        var linking = expected?.ElementAtOrDefault(1) == "link";
        var failurePage = linking ? "/admin/account" : "/admin/login";

        if (expected is null || code is null || state != expected[0])
        {
            return Redirect($"{failurePage}?slack=expired");
        }

        var identity = await SlackSignIn.Identify(code, SlackRedirectUri());
        if (identity is null)
        {
            return Redirect($"{failurePage}?slack=rejected");
        }

        if (!await SlackDirectory.IsAdmin(identity.UserId))
        {
            return Redirect($"{failurePage}?slack=not-admin");
        }

        var owner = await UserManager.FindByLoginAsync(SlackSignIn.LoginProvider, identity.UserId);
        if (!linking)
        {
            if (owner is null || await UserManager.IsLockedOutAsync(owner))
            {
                return Redirect("/admin/login?slack=unlinked");
            }

            await SignInManager.SignInAsync(owner, isPersistent: true);
            return Redirect("/admin/dashboard");
        }

        var user = await UserManager.GetUserAsync(User);
        if (user is null)
        {
            return Redirect("/admin/login");
        }

        if (owner is not null && owner.Id != user.Id)
        {
            return Redirect("/admin/account?slack=taken");
        }

        await RemoveSlackLogins(user);
        await UserManager.AddLoginAsync(user, new UserLoginInfo(SlackSignIn.LoginProvider, identity.UserId, identity.Name));
        return Redirect("/admin/account?slack=linked");
    }

    [HttpDelete("slack")]
    [Authorize]
    public async Task<IActionResult> UnlinkSlack()
    {
        var user = await UserManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        await RemoveSlackLogins(user);
        return await Me();
    }

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

        var slack = (await UserManager.GetLoginsAsync(user))
            .FirstOrDefault(login => login.LoginProvider == SlackSignIn.LoginProvider);

        return Ok(new
        {
            user.Email,
            Roles = await UserManager.GetRolesAsync(user),
            Slack = slack is null ? null : new { UserId = slack.ProviderKey, Name = slack.ProviderDisplayName },
        });
    }

    private async Task RemoveSlackLogins(User user)
    {
        foreach (var login in (await UserManager.GetLoginsAsync(user)).Where(login => login.LoginProvider == SlackSignIn.LoginProvider))
        {
            await UserManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        }
    }

    private string SlackRedirectUri() =>
        $"{(Site.BaseUrlConfigured ? Site.BaseUrl!.TrimEnd('/') : $"{Request.Scheme}://{Request.Host}")}/api/v0/auth/slack/callback";
}
