namespace devanewbot.Api.v0.Controllers;

using System;
using System.Linq;
using devanewbot.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RoushTech.Asio;

[Route("/api/v0/admin/logs")]
[Authorize(Roles = RoleSeeder.Administrators)]
public class AdminLogsController(AppLogBroadcaster broadcaster) : ControllerBase
{
    protected AppLogBroadcaster Broadcaster { get; } = broadcaster;

    [HttpGet]
    public IActionResult Get(
        [FromQuery] long afterSequence = 0,
        [FromQuery] string? level = null,
        [FromQuery] string? search = null,
        [FromQuery] int take = 500)
    {
        var buffered = Broadcaster.Snapshot();
        var oldest = buffered.Count == 0 ? 0 : buffered[0].Sequence;
        var newest = buffered.Count == 0 ? 0 : buffered[^1].Sequence;

        var entries = buffered.Where(entry => entry.Sequence > afterSequence);

        if (Enum.TryParse<LogLevel>(level, ignoreCase: true, out var minimum))
        {
            entries = entries.Where(entry => Severity(entry.Level) >= minimum);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            entries = entries.Where(entry =>
                entry.Message.Contains(term, StringComparison.OrdinalIgnoreCase)
                || entry.Category.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (entry.Exception?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return Ok(new
        {
            Entries = entries.TakeLast(Math.Clamp(take, 1, 2000)),
            Newest = newest,
            Buffered = buffered.Count,
            // A client that polls too slowly falls off the back of the ring; tell it what it lost.
            Missed = afterSequence > 0 && oldest > afterSequence + 1 ? oldest - afterSequence - 1 : 0
        });
    }

    private static LogLevel Severity(string level) =>
        Enum.TryParse<LogLevel>(level, ignoreCase: true, out var parsed) ? parsed : LogLevel.Information;
}
