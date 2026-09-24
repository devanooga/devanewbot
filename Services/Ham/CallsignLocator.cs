namespace devanewbot.Services.Ham;

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using devanewbot.Data;
using devanewbot.Data.Models;
using devanewbot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class CallsignLocator(
    IServiceScopeFactory scopeFactory,
    IMemoryCache memoryCache,
    IHttpClientFactory httpClientFactory,
    IOptions<HamAlertOptions> options,
    ILogger<CallsignLocator> logger)
{
    private const string ObservedSource = "observed";
    private const string HamQthSource = "hamqth";
    private static readonly TimeSpan MemoryTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan LookupTtl = TimeSpan.FromDays(30);

    private readonly SemaphoreSlim hamQthLock = new(1, 1);
    private string? hamQthSession;

    public async Task<string?> Grid(string callsign, CancellationToken cancellationToken)
    {
        var key = CacheKey(callsign);
        if (memoryCache.TryGetValue(key, out string? cached))
        {
            return cached;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevanewbotContext>();
        var stored = await db.HamCallsignLocations.FindAsync([callsign], cancellationToken);

        if (stored is not null && (stored.Grid is not null || DateTime.UtcNow - stored.UpdatedAt < LookupTtl))
        {
            return Remember(key, stored.Grid);
        }

        var grid = await LookupHamQth(callsign, cancellationToken);
        if (stored is null)
        {
            db.HamCallsignLocations.Add(new HamCallsignLocation
            {
                Callsign = callsign,
                Grid = grid,
                Source = HamQthSource,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            stored.Grid = grid;
            stored.Source = HamQthSource;
            stored.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Remember(key, grid);
    }

    public async Task Observed(string callsign, string? grid, CancellationToken cancellationToken)
    {
        var normalized = Maidenhead.Normalize(grid);
        if (normalized is null)
        {
            return;
        }

        var key = CacheKey(callsign);
        if (memoryCache.TryGetValue(key, out string? cached) && cached == normalized)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevanewbotContext>();
        var stored = await db.HamCallsignLocations.FindAsync([callsign], cancellationToken);

        if (stored is null)
        {
            db.HamCallsignLocations.Add(new HamCallsignLocation
            {
                Callsign = callsign,
                Grid = normalized,
                Source = ObservedSource,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else if (stored.Grid != normalized)
        {
            stored.Grid = normalized;
            stored.Source = ObservedSource;
            stored.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        Remember(key, normalized);
    }

    private string? Remember(string key, string? grid)
    {
        memoryCache.Set(key, grid, MemoryTtl);
        return grid;
    }

    private static string CacheKey(string callsign) => $"ham-grid:{callsign}";

    private async Task<string?> LookupHamQth(string callsign, CancellationToken cancellationToken)
    {
        var settings = options.Value.HamQth;
        if (!settings.Configured)
        {
            return null;
        }

        await hamQthLock.WaitAsync(cancellationToken);
        try
        {
            var client = httpClientFactory.CreateClient();
            hamQthSession ??= await HamQthLogin(client, settings, cancellationToken);

            var document = await HamQthQuery(client, callsign, cancellationToken);
            if (Element(document, "error") is { } error && error.Contains("Session", StringComparison.OrdinalIgnoreCase))
            {
                hamQthSession = await HamQthLogin(client, settings, cancellationToken);
                document = await HamQthQuery(client, callsign, cancellationToken);
            }

            return Maidenhead.Normalize(Element(document, "grid"));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "HamQTH lookup failed for {Callsign}", callsign);
            return null;
        }
        finally
        {
            hamQthLock.Release();
        }
    }

    private static async Task<string> HamQthLogin(HttpClient client, HamAlertOptions.HamQthOptions settings, CancellationToken cancellationToken)
    {
        var url = $"https://www.hamqth.com/xml.php?u={Uri.EscapeDataString(settings.Username!)}&p={Uri.EscapeDataString(settings.Password!)}";
        var document = XDocument.Parse(await client.GetStringAsync(url, cancellationToken));

        return Element(document, "session_id")
            ?? throw new InvalidOperationException($"HamQTH login failed: {Element(document, "error") ?? "no session id"}");
    }

    private async Task<XDocument> HamQthQuery(HttpClient client, string callsign, CancellationToken cancellationToken)
    {
        var url = $"https://www.hamqth.com/xml.php?id={hamQthSession}&callsign={Uri.EscapeDataString(callsign)}&prg=devanewbot";
        return XDocument.Parse(await client.GetStringAsync(url, cancellationToken));
    }

    private static string? Element(XDocument document, string name) =>
        document.Descendants().FirstOrDefault(element => element.Name.LocalName == name)?.Value.Trim();
}
