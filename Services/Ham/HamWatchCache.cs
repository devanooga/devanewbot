namespace devanewbot.Services.Ham;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using devanewbot.Data;
using devanewbot.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class HamWatchCache(IServiceScopeFactory scopeFactory)
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private IReadOnlyDictionary<string, HamWatch> watches = new Dictionary<string, HamWatch>();
    private DateTime refreshedAt = DateTime.MinValue;

    public void Invalidate() => refreshedAt = DateTime.MinValue;

    public async Task<IReadOnlyDictionary<string, HamWatch>> Snapshot(CancellationToken cancellationToken = default)
    {
        if (DateTime.UtcNow - refreshedAt < MaxAge)
        {
            return watches;
        }

        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (DateTime.UtcNow - refreshedAt < MaxAge)
            {
                return watches;
            }

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DevanewbotContext>();
            var list = await db.HamWatches.AsNoTracking().ToListAsync(cancellationToken);
            watches = list.ToDictionary(watch => watch.Callsign, StringComparer.OrdinalIgnoreCase);
            refreshedAt = DateTime.UtcNow;
            return watches;
        }
        finally
        {
            refreshLock.Release();
        }
    }
}
