namespace devanewbot.Services.Ham;

using System;
using System.Linq;
using System.Threading.Tasks;
using devanewbot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class HamSpotRetentionJob(DevanewbotContext db, IOptions<HamAlertOptions> options, ILogger<HamSpotRetentionJob> logger)
{
    public async Task Purge()
    {
        var cutoff = DateTime.UtcNow.AddDays(-options.Value.SpotRetentionDays);
        var deleted = await db.HamSpots.Where(spot => spot.CreatedAt < cutoff).ExecuteDeleteAsync();
        logger.LogInformation("Purged {Deleted} ham spots older than {Cutoff}", deleted, cutoff);
    }
}
