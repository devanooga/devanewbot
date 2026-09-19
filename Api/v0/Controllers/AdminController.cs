namespace devanewbot.Api.v0.Controllers;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using devanewbot.Api.v0.Models.Admin;
using devanewbot.Data;
using devanewbot.Models;
using devanewbot.Seeders;
using devanewbot.Services.Ham;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

[Route("/api/v0/admin")]
[Authorize(Roles = RoleSeeder.Administrators)]
public partial class AdminController(
    DevanewbotContext db,
    HamFeedStatus feeds,
    HamWatchService watchList,
    IOptions<HamAlertOptions> hamAlertOptions,
    JobStorage jobStorage) : ControllerBase
{
    [GeneratedRegex("^[A-Z0-9]{3,12}$")]
    private static partial Regex CallsignPattern { get; }

    protected DevanewbotContext Db { get; } = db;
    protected HamFeedStatus Feeds { get; } = feeds;
    protected HamWatchService WatchList { get; } = watchList;
    protected JobStorage JobStorage { get; } = jobStorage;

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var dayAgo = DateTime.UtcNow.AddDays(-1);
        using var connection = JobStorage.GetConnection();
        var statistics = JobStorage.GetMonitoringApi().GetStatistics();

        return Ok(new
        {
            Problems = Problems(),
            Feeds = Feeds.Snapshot(),
            Watches = await Watches(),
            Sessions = await Sessions(),
            Spots = new
            {
                Total = await Db.HamSpots.LongCountAsync(),
                LastDay = await Db.HamSpots.LongCountAsync(spot => spot.CreatedAt >= dayAgo),
                BySource = await Db.HamSpots
                    .Where(spot => spot.CreatedAt >= dayAgo)
                    .GroupBy(spot => spot.Source)
                    .Select(group => new { Source = group.Key, Count = group.LongCount() })
                    .ToListAsync()
            },
            Jobs = new
            {
                statistics.Enqueued,
                statistics.Processing,
                statistics.Scheduled,
                statistics.Succeeded,
                statistics.Failed,
                Recurring = connection.GetRecurringJobs().Select(job => new
                {
                    job.Id,
                    job.Cron,
                    job.LastExecution,
                    job.NextExecution,
                    job.LastJobState,
                    job.Error
                })
            }
        });
    }

    [HttpGet("sessions/{id}")]
    public async Task<IActionResult> Session([FromRoute] Guid id)
    {
        var session = await Db.HamSpotSessions
            .Where(session => session.Id == id)
            .Select(session => new
            {
                session.Id,
                session.Callsign,
                session.Band,
                session.Mode,
                session.Grid,
                OpenedAt = session.CreatedAt,
                session.LastHeardAt,
                session.ClosedAt,
                session.SlackChannelId,
                session.SlackMessageTs,
                Spots = session.Spots
                    .OrderByDescending(spot => spot.HeardAt)
                    .Take(200)
                    .Select(spot => new
                    {
                        spot.Id,
                        spot.Source,
                        spot.Reporter,
                        spot.ReporterGrid,
                        spot.ReporterCountry,
                        spot.FrequencyHz,
                        spot.Snr,
                        spot.Wpm,
                        spot.DistanceKm,
                        spot.Comment,
                        spot.HeardAt
                    })
            })
            .SingleOrDefaultAsync();

        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("sessions/{id}/reannounce")]
    public async Task<IActionResult> Reannounce([FromRoute] Guid id)
    {
        var session = await Db.HamSpotSessions.FindAsync(id);
        if (session is null)
        {
            return NotFound();
        }

        session.SlackChannelId = null;
        session.SlackMessageTs = null;
        session.RenderedAt = null;
        session.Suppressed = false;
        session.ForcedAnnounce = true;
        session.UpdatedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("watches")]
    public async Task<IActionResult> AddWatch([FromBody] WatchModel model)
    {
        var callsign = model.Callsign?.Trim().ToUpperInvariant();
        if (callsign is null || !CallsignPattern.IsMatch(callsign))
        {
            return BadRequest(new { Errors = new[] { "Not a callsign." } });
        }

        if (model.Grid is not null && model.Grid.Length > 0 && !Maidenhead.IsValid(model.Grid))
        {
            return BadRequest(new { Errors = new[] { "Not a Maidenhead grid." } });
        }

        var added = await WatchList.Add([callsign], model.Grid, User.Identity?.Name ?? "admin");
        if (added.Length == 0)
        {
            return BadRequest(new { Errors = new[] { $"{callsign} is already on the watch list." } });
        }

        if (!string.IsNullOrWhiteSpace(model.SlackUserId))
        {
            await WatchList.Update(callsign, model.Grid, model.SlackUserId);
        }

        return Ok(await Watches());
    }

    [HttpPut("watches/{callsign}")]
    public async Task<IActionResult> UpdateWatch([FromRoute] string callsign, [FromBody] WatchModel model)
    {
        if (model.Grid is not null && model.Grid.Length > 0 && !Maidenhead.IsValid(model.Grid))
        {
            return BadRequest(new { Errors = new[] { "Not a Maidenhead grid." } });
        }

        return await WatchList.Update(callsign, model.Grid, model.SlackUserId) ? Ok(await Watches()) : NotFound();
    }

    [HttpDelete("watches/{callsign}")]
    public async Task<IActionResult> RemoveWatch([FromRoute] string callsign)
    {
        var removed = await WatchList.Remove([callsign]);
        return removed.Length == 0 ? NotFound() : Ok(await Watches());
    }

    private async Task<object> Sessions()
    {
        var staleAfter = hamAlertOptions.Value.StaleAfterMinutes;

        var recent = await Db.HamSpotSessions
            .OrderByDescending(session => session.LastHeardAt)
            .Take(25)
            .Select(session => new
            {
                session.Id,
                session.Callsign,
                session.Band,
                session.Mode,
                session.Grid,
                OpenedAt = session.CreatedAt,
                session.LastHeardAt,
                session.ClosedAt,
                session.Suppressed,
                session.SpotCount,
                session.ReporterCount,
                session.FurthestKm,
                session.FurthestReporter,
                session.BestSnr,
                session.BestSnrReporter
            })
            .ToListAsync();

        return recent
            .Select(session => new
            {
                session.Id,
                session.Callsign,
                session.Band,
                session.Mode,
                session.Grid,
                session.OpenedAt,
                session.LastHeardAt,
                session.ClosedAt,
                session.Suppressed,
                session.SpotCount,
                session.ReporterCount,
                session.FurthestKm,
                session.FurthestReporter,
                session.BestSnr,
                session.BestSnrReporter,
                State = HamSessionLifecycle.Of(session.LastHeardAt, session.ClosedAt, staleAfter)
                    .ToString()
                    .ToLowerInvariant()
            })
            .ToList();
    }

    private string[] Problems() =>
        hamAlertOptions.Value.ChannelConfigured
            ? []
            : ["HamAlert:ChannelId is not set, so sessions are tracked but never announced in Slack."];

    private async Task<object> Watches() =>
        await Db.HamWatches
            .OrderBy(watch => watch.Callsign)
            .Select(watch => new { watch.Callsign, watch.Grid, watch.SlackUserId, watch.AddedBy, watch.CreatedAt })
            .ToListAsync();
}
