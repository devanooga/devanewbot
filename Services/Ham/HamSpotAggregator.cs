namespace devanewbot.Services.Ham;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using devanewbot.Data;
using devanewbot.Data.Models;
using devanewbot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class HamSpotAggregator(
    HamSpotQueue queue,
    HamWatchCache watches,
    CallsignLocator locator,
    CountryFile countries,
    HamSessionMessenger messenger,
    IServiceScopeFactory scopeFactory,
    IMemoryCache memoryCache,
    IOptions<HamAlertOptions> options,
    ILogger<HamSpotAggregator> logger) : BackgroundService
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(10);

    private readonly SemaphoreSlim gate = new(1, 1);

    private TimeSpan IdleTimeout => TimeSpan.FromMinutes(options.Value.IdleTimeoutMinutes);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.ChannelConfigured)
        {
            logger.LogError("HamAlert:ChannelId is not set, so sessions will be tracked but never announced");
        }

        await countries.Refresh(stoppingToken);
        await Task.WhenAll(Consume(stoppingToken), Maintain(stoppingToken));
    }

    private async Task Consume(CancellationToken stoppingToken)
    {
        await foreach (var spot in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await Process(spot, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Failed to process spot of {Callsign} from {Source}", spot.Callsign, spot.Source);
            }
        }
    }

    private async Task Maintain(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.UpdateIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await gate.WaitAsync(stoppingToken);
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DevanewbotContext>();
                var watched = await watches.Snapshot(stoppingToken);
                var now = DateTime.UtcNow;
                var cutoff = now - IdleTimeout;
                var staleAfter = options.Value.StaleAfterMinutes;

                // Closed sessions stay in scope so one that failed to announce still gets a message, and a
                // quiet session is re-rendered every tick so its silence counter keeps counting.
                var pending = await db.HamSpotSessions
                    .Where(session =>
                        (session.ClosedAt == null && session.LastHeardAt < cutoff)
                        || session.RenderedAt == null
                        || session.UpdatedAt > session.RenderedAt
                        || (session.ClosedAt == null && !session.Suppressed && session.LastHeardAt.AddMinutes(staleAfter) <= now))
                    .ToListAsync(stoppingToken);

                foreach (var session in pending)
                {
                    if (session.ClosedAt == null && session.LastHeardAt < cutoff)
                    {
                        session.ClosedAt = DateTime.UtcNow;
                    }

                    try
                    {
                        await Render(db, session, watched.GetValueOrDefault(session.Callsign)?.SlackUserId, stoppingToken);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        logger.LogError(exception, "Could not render {Callsign} on {Band} {Mode}", session.Callsign, session.Band, session.Mode);
                    }
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Session maintenance failed");
            }
            finally
            {
                gate.Release();
            }
        }
    }

    private async Task Process(ReceivedSpot spot, CancellationToken cancellationToken)
    {
        var watched = await watches.Snapshot(cancellationToken);
        if (!watched.TryGetValue(spot.Callsign, out var watch) || IsDuplicate(spot))
        {
            return;
        }

        await locator.Observed(spot.Callsign, spot.SpottedGrid, cancellationToken);
        await locator.Observed(spot.Reporter, spot.ReporterGrid, cancellationToken);

        var spottedGrid = Maidenhead.Normalize(spot.SpottedGrid) ?? watch.Grid ?? await locator.Grid(spot.Callsign, cancellationToken);
        var reporterGrid = Maidenhead.Normalize(spot.ReporterGrid) ?? await locator.Grid(spot.Reporter, cancellationToken);

        await gate.WaitAsync(cancellationToken);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DevanewbotContext>();

            var session = await db.HamSpotSessions.FirstOrDefaultAsync(
                session => session.Callsign == spot.Callsign && session.Band == spot.Band && session.Mode == spot.Mode && session.ClosedAt == null,
                cancellationToken);

            if (session is not null && ShouldSplit(session, spot, spottedGrid))
            {
                session.ClosedAt = DateTime.UtcNow;
                await Render(db, session, watch.SlackUserId, cancellationToken);
                session = null;
            }

            var opened = session is null;
            session ??= db.HamSpotSessions.Add(new HamSpotSession
            {
                Callsign = spot.Callsign,
                Band = spot.Band,
                Mode = spot.Mode,
                Grid = spottedGrid,
                CreatedAt = spot.HeardAt,
                LastHeardAt = spot.HeardAt
            }).Entity;

            session.Grid ??= spottedGrid;
            session.LastHeardAt = spot.HeardAt > session.LastHeardAt ? spot.HeardAt : session.LastHeardAt;
            session.UpdatedAt = DateTime.UtcNow;

            db.HamSpots.Add(new HamSpot
            {
                Session = session,
                Source = spot.Source,
                Callsign = spot.Callsign,
                Reporter = spot.Reporter,
                ReporterGrid = reporterGrid,
                ReporterCountry = countries.Lookup(spot.Reporter)?.Name,
                SpottedGrid = spottedGrid,
                FrequencyHz = spot.FrequencyHz,
                Band = spot.Band,
                Mode = spot.Mode,
                Snr = spot.Snr,
                Wpm = spot.Wpm,
                DistanceKm = Maidenhead.DistanceKm(spottedGrid, reporterGrid),
                Comment = spot.Comment,
                HeardAt = spot.HeardAt
            });

            await db.SaveChangesAsync(cancellationToken);

            if (opened)
            {
                await Render(db, session, watch.SlackUserId, cancellationToken);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private bool IsDuplicate(ReceivedSpot spot)
    {
        var key = $"ham-spot:{spot.Reporter}|{spot.Callsign}|{spot.Band}|{spot.Mode}|{spot.HeardAt:yyyyMMddHHmm}";
        if (memoryCache.TryGetValue(key, out _))
        {
            return true;
        }

        memoryCache.Set(key, true, DuplicateWindow);
        return false;
    }

    private bool ShouldSplit(HamSpotSession session, ReceivedSpot spot, string? spottedGrid)
    {
        if (spot.HeardAt - session.LastHeardAt > IdleTimeout)
        {
            return true;
        }

        return Maidenhead.DistanceKm(session.Grid, spottedGrid) is { } moved && moved > options.Value.LocationChangeKm;
    }

    /// A handful of misconfigured PSK Reporter receivers report the wrong band, which lands a watched
    /// operator in a second session with a single spot while their real one has hundreds. A session is
    /// held back only when a concurrent session for the same callsign dwarfs it by this much.
    /// The ceiling matters as much as the ratio: bogus bands come from one or two stray receivers, so a
    /// session with real breadth is never held back however busy the operator's main band is.
    private static bool IsMinority(int reporters, int rivalReporters, int ratio, int ceiling) =>
        ratio > 0
        && rivalReporters > 0
        && reporters <= ceiling
        && reporters * ratio <= rivalReporters;

    private async Task<int> RivalReporters(DevanewbotContext db, HamSpotSession session, CancellationToken cancellationToken)
    {
        var window = TimeSpan.FromMinutes(options.Value.MinorityWindowMinutes);
        var from = session.CreatedAt - window;
        var to = session.LastHeardAt + window;

        // Overlapping spans, not nearby last-heard times: a rival that keeps running slides its last-heard
        // past any window anchored on ours, which would release the session it is meant to hold back.
        return await db.HamSpotSessions
            .Where(rival => rival.Callsign == session.Callsign
                && rival.Id != session.Id
                && rival.LastHeardAt >= from
                && rival.CreatedAt <= to)
            .Select(rival => (int?)rival.ReporterCount)
            .MaxAsync(cancellationToken) ?? 0;
    }

    /// PSK Reporter and the skimmer networks report continuously, so a session that only ever drew one or
    /// two of them is a lone receiver hearing a stray decode rather than someone on the air. A POTA, SOTA
    /// or cluster spot is a person deliberately spotting an operator, and one of those is worth announcing.
    private bool BelowMinimumReporters(HamSessionView view) =>
        view.ReporterCount < options.Value.MinimumReporters
        && view.Sources.All(IsAutomated);

    private bool IsAutomated(string source) =>
        source == PskReporterFeed.Name
        || options.Value.TelnetNodes.Any(node => node.Automated && node.Name == source);

    private async Task<string?> HoldBack(DevanewbotContext db, HamSpotSession session, HamSessionView view, CancellationToken cancellationToken)
    {
        if (BelowMinimumReporters(view))
        {
            return $"{view.ReporterCount} automated reporter(s), under the {options.Value.MinimumReporters} needed to announce";
        }

        var rivals = await RivalReporters(db, session, cancellationToken);
        if (IsMinority(view.ReporterCount, rivals, options.Value.MinorityDominanceRatio, options.Value.MinorityReporterCeiling))
        {
            return $"{view.ReporterCount} reporter(s) against a concurrent session for the same callsign";
        }

        return null;
    }

    private async Task Render(DevanewbotContext db, HamSpotSession session, string? slackUserId, CancellationToken cancellationToken)
    {
        var view = await BuildView(db, session, slackUserId, options.Value.StaleAfterMinutes, cancellationToken);

        session.SpotCount = view.SpotCount;
        session.ReporterCount = view.ReporterCount;
        session.FurthestKm = view.Furthest?.DistanceKm;
        session.FurthestReporter = view.Furthest?.Callsign;
        session.BestSnr = view.Loudest?.Snr;
        session.BestSnrReporter = view.Loudest?.Callsign;
        session.RenderedAt = DateTime.UtcNow;

        // An announced session keeps its message; only a first announcement can be held back.
        var announced = session.SlackMessageTs is not null && session.SlackChannelId is not null;
        var heldBack = announced || session.ForcedAnnounce
            ? null
            : await HoldBack(db, session, view, cancellationToken);

        session.Suppressed = heldBack is not null;

        if (heldBack is not null)
        {
            logger.LogInformation(
                "Holding back {Callsign} on {Band} {Mode}: {Reason}",
                session.Callsign, session.Band, session.Mode, heldBack);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            if (!announced)
            {
                (session.SlackChannelId, session.SlackMessageTs) = await messenger.Post(view);
            }
            else
            {
                await messenger.Update(session.SlackChannelId, session.SlackMessageTs, view);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Could not post Slack update for {Callsign} on {Band} {Mode}", session.Callsign, session.Band, session.Mode);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<HamSessionView> BuildView(DevanewbotContext db, HamSpotSession session, string? slackUserId, int staleAfterMinutes, CancellationToken cancellationToken)
    {
        var spots = db.HamSpots.Where(spot => spot.SessionId == session.Id);

        var furthest = await spots
            .Where(spot => spot.DistanceKm != null)
            .OrderByDescending(spot => spot.DistanceKm)
            .Select(spot => new HamSessionView.Reporter(spot.Reporter, spot.ReporterCountry, spot.DistanceKm, spot.Snr))
            .FirstOrDefaultAsync(cancellationToken);

        var loudest = await spots
            .Where(spot => spot.Snr != null)
            .OrderByDescending(spot => spot.Snr)
            .Select(spot => new HamSessionView.Reporter(spot.Reporter, spot.ReporterCountry, spot.DistanceKm, spot.Snr))
            .FirstOrDefaultAsync(cancellationToken);

        var countryCounts = await spots
            .Where(spot => spot.ReporterCountry != null)
            .Select(spot => new { Country = spot.ReporterCountry!, spot.Reporter })
            .Distinct()
            .GroupBy(pair => pair.Country)
            .Select(group => new { Country = group.Key, Reporters = group.Count() })
            .OrderByDescending(country => country.Reporters)
            .ThenBy(country => country.Country)
            .Take(8)
            .ToListAsync(cancellationToken);

        var countries = countryCounts
            .Select(country => new HamSessionView.CountryCount(country.Country, country.Reporters))
            .ToList();

        var latest = await spots
            .OrderByDescending(spot => spot.HeardAt)
            .Select(spot => new { spot.FrequencyHz, spot.Comment })
            .FirstOrDefaultAsync(cancellationToken);

        var comment = latest?.Comment ?? await spots
            .Where(spot => spot.Comment != null)
            .OrderByDescending(spot => spot.HeardAt)
            .Select(spot => spot.Comment)
            .FirstOrDefaultAsync(cancellationToken);

        return new HamSessionView(
            Callsign: session.Callsign,
            Band: session.Band,
            Mode: session.Mode,
            Grid: session.Grid,
            SlackUserId: slackUserId,
            FrequencyHz: latest?.FrequencyHz,
            OpenedAt: session.CreatedAt,
            LastHeardAt: session.LastHeardAt,
            ClosedAt: session.ClosedAt,
            State: HamSessionLifecycle.Of(session.LastHeardAt, session.ClosedAt, staleAfterMinutes),
            SpotCount: await spots.CountAsync(cancellationToken),
            ReporterCount: await spots.Select(spot => spot.Reporter).Distinct().CountAsync(cancellationToken),
            Furthest: furthest,
            Loudest: loudest,
            Countries: countries,
            Sources: await spots.Select(spot => spot.Source).Distinct().OrderBy(source => source).ToListAsync(cancellationToken),
            Comment: comment);
    }
}
