namespace devanewbot.Services;

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using devanewbot.Data.Models;
using Microsoft.AspNetCore.Identity;

public class AdminIdentity(UserManager<User> userManager, SlackDirectory slackDirectory)
{
    public async Task<string?> LinkedSlackUserId(ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        return user is null
            ? null
            : (await userManager.GetLoginsAsync(user))
                .FirstOrDefault(login => login.LoginProvider == SlackSignIn.LoginProvider)?.ProviderKey;
    }

    public async Task<string> Name(ClaimsPrincipal principal) =>
        await LinkedSlackUserId(principal) is { } slackUserId
            ? await slackDirectory.Name(slackUserId)
            : (await userManager.GetUserAsync(principal))?.Email ?? "admin";
}
