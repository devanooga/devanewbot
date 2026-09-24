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

[Route("/api/v0/admin/invites")]
[Authorize(Roles = RoleSeeder.Administrators)]
public class AdminInvitesController(DevanewbotContext db, InviteService inviteService) : ControllerBase
{
    protected DevanewbotContext Db { get; } = db;
    protected InviteService InviteService { get; } = inviteService;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] InviteStatus? status, [FromQuery] string? search, [FromQuery] int take = 200)
    {
        var query = Db.Invites.AsNoTracking();
        if (status is { } filter)
        {
            query = query.Where(invite => invite.Status == filter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(invite => invite.Email.ToLower().Contains(term) || invite.Ip.Contains(term));
        }

        return Ok(await query
            .OrderByDescending(invite => invite.CreatedAt)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(invite => new
            {
                invite.Id,
                invite.Email,
                invite.Ip,
                Source = invite.Source.ToString(),
                Status = invite.Status.ToString(),
                invite.Flag,
                invite.FlagMessage,
                invite.City,
                invite.Region,
                invite.Country,
                invite.Isp,
                invite.Proxy,
                invite.Hosting,
                invite.Mobile,
                invite.CreatedAt,
                invite.DecidedAt,
                invite.DecidedBy,
                invite.DecidedBySlackUserId,
                DecisionSource = invite.DecisionSource.ToString(),
                invite.Error
            })
            .ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        var invite = await Db.Invites.AsNoTracking().SingleOrDefaultAsync(invite => invite.Id == id);
        return invite is null ? NotFound() : Ok(invite);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InviteModel model)
    {
        var email = model.Email.Trim();
        if (!email.Contains('@'))
        {
            return BadRequest(new { Errors = new[] { "Not an email address." } });
        }

        return await Run(() => InviteService.CreateAdminInvite(email, User.Identity?.Name ?? "admin"));
    }

    [HttpPost("{id}/approve")]
    public Task<IActionResult> Approve([FromRoute] Guid id) =>
        Run(() => InviteService.DecideFromAdmin(id, approve: true, User.Identity?.Name ?? "admin"));

    [HttpPost("{id}/decline")]
    public Task<IActionResult> Decline([FromRoute] Guid id) =>
        Run(() => InviteService.DecideFromAdmin(id, approve: false, User.Identity?.Name ?? "admin"));

    private async Task<IActionResult> Run(Func<Task<InviteResult>> action)
    {
        try
        {
            return Ok(new { Result = (await action()).ToString() });
        }
        catch (Exception exception)
        {
            return BadRequest(new { Errors = new[] { exception.Message } });
        }
    }
}
