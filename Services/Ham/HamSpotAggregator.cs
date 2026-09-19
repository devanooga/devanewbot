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
                var cutoff = DateTime.UtcNow - IdleTimeout;

                var open = await db.HamSpotSessions
                    .Where(session => session.ClosedAt == null)
                    .Where(session => session.LastHeardAt < cutoff || session.RenderedAt == null || session.UpdatedAt > session.RenderedAt)
                    .ToListAsync(stoppingToken);

                foreach (var session in open)
                {
                    if (session.LastHeardAt < cutoff)
                    {
                        session.ClosedAt = DateTime.UtcNow;
                    }

                    await Render(db, session, watched.GetValueOrDefault(session.Callsign)?.SlackUserId, stoppingToken);
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

    private async Task Render(DevanewbotContext db, HamSpotSession session, string? slackUserId, CancellationToken cancellationToken)
    {
        var view = await BuildView(db, session, slackUserId, cancellationToken);

        session.SpotCount = view.SpotCount;
        session.ReporterCount = view.ReporterCount;
        session.FurthestKm = view.Furthest?.DistanceKm;
        session.FurthestReporter = view.Furthest?.Callsign;
        session.BestSnr = view.Loudest?.Snr;
        session.BestSnrReporter = view.Loudest?.Callsign;
        session.RenderedAt = DateTime.UtcNow;

        try
        {
            if (session.SlackMessageTs is null || session.SlackChannelId is null)
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

    private static async Task<HamSessionView> BuildView(DevanewbotContext db, HamSpotSession session, string? slackUserId, CancellationToken cancellationToken)
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

        var countries = await spots
            .Where(spot => spot.ReporterCountry != null)
            .GroupBy(spot => spot.ReporterCountry!)
            .Select(group => new HamSessionView.CountryCount(group.Key, group.Select(spot => spot.Reporter).Distinct().Count()))
            .OrderByDescending(country => country.Reporters)
            .ThenBy(country => country.Country)
            .Take(8)
            .ToListAsync(cancellationToken);

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
            SpotCount: await spots.CountAsync(cancellationToken),
            ReporterCount: await spots.Select(spot => spot.Reporter).Distinct().CountAsync(cancellationToken),
            Furthest: furthest,
            Loudest: loudest,
            Countries: countries,
            Sources: await spots.Select(spot => spot.Source).Distinct().OrderBy(source => source).ToListAsync(cancellationToken),
            Comment: comment);
    }
}
