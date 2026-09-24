namespace devanewbot.Services.Ham;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using devanewbot.Data;
using devanewbot.Data.Models;
using devanewbot.Models;
using Microsoft.EntityFrameworkCore;

public class HamWatchService(DevanewbotContext db, HamWatchCache cache)
{
    public Task<List<HamWatch>> List() =>
        db.HamWatches.AsNoTracking().OrderBy(watch => watch.Callsign).ToListAsync();

    public async Task<string[]> Add(IEnumerable<string> callsigns, string? grid, string addedBy)
    {
        var requested = callsigns.Select(Normalize).Distinct().ToArray();
        var existing = await db.HamWatches
            .Where(watch => requested.Contains(watch.Callsign))
            .Select(watch => watch.Callsign)
            .ToListAsync();
        var added = requested.Except(existing).ToArray();

        foreach (var callsign in added)
        {
            db.HamWatches.Add(new HamWatch
            {
                Callsign = callsign,
                Grid = Maidenhead.Normalize(grid),
                AddedBy = addedBy
            });
        }

        await Commit();
        return added;
    }

    public async Task<string[]> Remove(IEnumerable<string> callsigns)
    {
        var requested = callsigns.Select(Normalize).Distinct().ToArray();
        var matches = await db.HamWatches.Where(watch => requested.Contains(watch.Callsign)).ToListAsync();

        db.HamWatches.RemoveRange(matches);
        await Commit();
        return [.. matches.Select(watch => watch.Callsign)];
    }

    public async Task<bool> SetGrid(string callsign, string grid)
    {
        var watch = await db.HamWatches.FirstOrDefaultAsync(watch => watch.Callsign == Normalize(callsign));
        if (watch is null)
        {
            return false;
        }

        watch.Grid = Maidenhead.Normalize(grid);
        await Commit();
        return true;
    }

    public async Task<bool> Update(string callsign, string? grid, string? slackUserId)
    {
        var watch = await db.HamWatches.FirstOrDefaultAsync(watch => watch.Callsign == Normalize(callsign));
        if (watch is null)
        {
            return false;
        }

        watch.Grid = Maidenhead.Normalize(grid);
        watch.SlackUserId = string.IsNullOrWhiteSpace(slackUserId) ? null : slackUserId.Trim();
        await Commit();
        return true;
    }

    public async Task<bool> Claim(string callsign, string slackUserId)
    {
        var watch = await db.HamWatches.FirstOrDefaultAsync(watch => watch.Callsign == Normalize(callsign));
        if (watch is null)
        {
            return false;
        }

        watch.SlackUserId = slackUserId;
        await Commit();
        return true;
    }

    private async Task Commit()
    {
        await db.SaveChangesAsync();
        cache.Invalidate();
    }

    private static string Normalize(string callsign) => callsign.Trim().ToUpperInvariant();
}
