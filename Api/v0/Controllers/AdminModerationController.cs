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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("/api/v0/admin/moderation")]
[Authorize(Roles = RoleSeeder.Administrators)]
public class AdminModerationController(
    DevanewbotContext db,
    ModerationLog moderationLog,
    MessageRemoval messageRemoval,
    SlackDirectory slackDirectory,
    AdminIdentity adminIdentity) : ControllerBase
{
    protected DevanewbotContext Db { get; } = db;
    protected ModerationLog ModerationLog { get; } = moderationLog;
    protected MessageRemoval MessageRemoval { get; } = messageRemoval;
    protected SlackDirectory SlackDirectory { get; } = slackDirectory;
    protected AdminIdentity AdminIdentity { get; } = adminIdentity;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? kind = null, [FromQuery] string? search = null)
    {
        var actions = Db.ModerationActions.AsNoTracking();

        if (Enum.TryParse<ModerationActionKind>(kind, ignoreCase: true, out var parsedKind))
        {
            actions = actions.Where(action => action.Kind == parsedKind);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            actions = actions.Where(action =>
                EF.Functions.ILike(action.Action, pattern)
                || EF.Functions.ILike(action.Reason, pattern)
                || EF.Functions.ILike(action.Administrator, pattern)
                || (action.RemovedMessageText != null && EF.Functions.ILike(action.RemovedMessageText, pattern)));
        }

        return Ok(await actions.OrderByDescending(action => action.OccurredAt).Take(1000).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ModerationActionModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Action))
        {
            return Error("Say what was done.");
        }

        await ModerationLog.Record(new ModerationAction
        {
            OccurredAt = model.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow,
            Kind = ModerationActionKind.Other,
            Source = ModerationActionSource.Admin,
            Action = model.Action.Trim(),
            Reason = model.Reason?.Trim() ?? "",
            Administrator = await AdminIdentity.Name(User),
            AdministratorSlackUserId = await AdminIdentity.LinkedSlackUserId(User)
        });

        return await List();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] ModerationActionModel model)
    {
        var action = await Db.ModerationActions.FindAsync(id);
        if (action is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(model.Action))
        {
            return Error("Say what was done.");
        }

        action.Action = model.Action.Trim();
        action.Reason = model.Reason?.Trim() ?? "";
        if (model.OccurredAt is { } occurredAt)
        {
            action.OccurredAt = occurredAt.ToUniversalTime();
        }

        await Db.SaveChangesAsync();
        return await List();
    }

    [HttpPost("{id}/hide")]
    public async Task<IActionResult> Hide([FromRoute] Guid id)
    {
        var action = await Db.ModerationActions.FindAsync(id);
        if (action is null)
        {
            return NotFound();
        }

        action.HiddenAt = DateTime.UtcNow;
        action.HiddenBy = await AdminIdentity.Name(User);
        await Db.SaveChangesAsync();
        return await List();
    }

    [HttpPost("{id}/unhide")]
    public async Task<IActionResult> Unhide([FromRoute] Guid id)
    {
        var action = await Db.ModerationActions.FindAsync(id);
        if (action is null)
        {
            return NotFound();
        }

        action.HiddenAt = null;
        action.HiddenBy = null;
        await Db.SaveChangesAsync();
        return await List();
    }

    [HttpGet("messages")]
    public async Task<IActionResult> Messages([FromQuery] string userId, [FromQuery] int hours = 24)
    {
        var window = TimeSpan.FromHours(Math.Clamp(hours, 1, 24 * 30));
        try
        {
            return Ok(new
            {
                Author = await SlackDirectory.Name(userId),
                AuthorIsAdmin = await SlackDirectory.IsAdmin(userId),
                Messages = await MessageRemoval.Recent(userId, window)
            });
        }
        catch (SlackNet.SlackException e)
        {
            return Error($"Slack search failed ({e.ErrorCode}).");
        }
    }

    [HttpPost("removals")]
    public async Task<IActionResult> Remove([FromBody] RemovalModel model)
    {
        if (string.IsNullOrWhiteSpace(model.UserId) || string.IsNullOrWhiteSpace(model.Reason))
        {
            return Error("Give a reason.");
        }

        if (model.Messages.Count == 0 && !model.Deactivate)
        {
            return Error("Pick at least one message.");
        }

        var administratorId = await AdminIdentity.LinkedSlackUserId(User);
        if (administratorId is null)
        {
            return Error("Link your Slack account on the Account page first.");
        }

        if (!await SlackDirectory.IsAdmin(administratorId))
        {
            return Error("Only Slack workspace admins can remove messages.");
        }

        if (model.Deactivate && await SlackDirectory.IsAdmin(model.UserId))
        {
            return Error("Admins cannot be deactivated from here.");
        }

        return Ok(await MessageRemoval.Remove(administratorId, model.UserId, model.Messages, model.Reason.Trim(), model.Deactivate));
    }

    private BadRequestObjectResult Error(string message) => BadRequest(new { Errors = new[] { message } });
}
