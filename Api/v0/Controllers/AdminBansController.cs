namespace devanewbot.Api.v0.Controllers;

using System;
using System.Linq;
using System.Threading.Tasks;
using devanewbot.Api.v0.Models.Admin;
using devanewbot.Data;
using devanewbot.Data.Models;
using devanewbot.Seeders;
using devanewbot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("/api/v0/admin/bans")]
[Authorize(Roles = RoleSeeder.Administrators)]
public class AdminBansController(
    DevanewbotContext db,
    IChannelBanService channelBanService,
    SlackDirectory slackDirectory,
    UserManager<User> userManager) : ControllerBase
{
    protected DevanewbotContext Db { get; } = db;
    protected IChannelBanService ChannelBans { get; } = channelBanService;
    protected SlackDirectory SlackDirectory { get; } = slackDirectory;
    protected UserManager<User> UserManager { get; } = userManager;

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await Db.ChannelBans
            .AsNoTracking()
            .OrderByDescending(ban => ban.Active)
            .ThenByDescending(ban => ban.BannedAt)
            .Take(500)
            .ToListAsync());

    [HttpGet("directory")]
    public async Task<IActionResult> Directory() =>
        Ok(new { People = await SlackDirectory.People(), Channels = await SlackDirectory.Channels() });

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BanModel model)
    {
        if (string.IsNullOrWhiteSpace(model.UserId) || string.IsNullOrWhiteSpace(model.ChannelId) || string.IsNullOrWhiteSpace(model.Reason))
        {
            return Error("Pick a person and a channel, and give a reason.");
        }

        if (model.ExpiresOn is { } expiresOn && expiresOn <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Error("The ban has to end after today.");
        }

        var banningUserId = await LinkedSlackUserId();
        if (banningUserId is null)
        {
            return Error("Link your Slack account on the Account page first.");
        }

        if (!await SlackDirectory.IsAdmin(banningUserId))
        {
            return Error("Only Slack workspace admins can ban people.");
        }

        if (await ChannelBans.HasActiveBan(model.ChannelId, model.UserId))
        {
            return Error("They are already banned from that channel.");
        }

        await ChannelBans.AddBan(
            model.ChannelId,
            model.UserId,
            banningUserId,
            model.Reason.Trim(),
            model.ExpiresOn?.ToDateTime(TimeOnly.MinValue));
        return await List();
    }

    [HttpPost("{id}/lift")]
    public async Task<IActionResult> Lift([FromRoute] Guid id)
    {
        var ban = await Db.ChannelBans.AsNoTracking().SingleOrDefaultAsync(ban => ban.Id == id);
        if (ban is null)
        {
            return NotFound();
        }

        if (!ban.Active)
        {
            return Error("That ban is no longer active.");
        }

        var liftingUserId = await LinkedSlackUserId();
        if (liftingUserId is null)
        {
            return Error("Link your Slack account on the Account page first.");
        }

        if (!await SlackDirectory.IsAdmin(liftingUserId))
        {
            return Error("Only Slack workspace admins can lift bans.");
        }

        await ChannelBans.RemoveBan(ban.ChannelId, ban.UserId);
        return await List();
    }

    private async Task<string?> LinkedSlackUserId()
    {
        var user = await UserManager.GetUserAsync(User);
        return user is null
            ? null
            : (await UserManager.GetLoginsAsync(user))
                .FirstOrDefault(login => login.LoginProvider == SlackSignIn.LoginProvider)?.ProviderKey;
    }

    private BadRequestObjectResult Error(string message) => BadRequest(new { Errors = new[] { message } });
}
