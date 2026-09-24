namespace devanewbot.Services.Ham;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using devanewbot.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// SOTA requires every API consumer to register a point of contact with their management team before
// connecting, so this feed stays off until HamAlert:Sota:Url is filled in with the endpoint they issue.
public class SotaFeed(
    HamSpotQueue queue,
    HamWatchCache watches,
    HamFeedStatus status,
    IHttpClientFactory httpClientFactory,
    IOptions<HamAlertOptions> options,
    ILogger<SotaFeed> logger) : BackgroundService
{
    public const string Name = "SOTA";

    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(10);

    private HashSet<long> seenSpotIds = [];

    private class SotaSpot
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("timeStamp")] public DateTime? TimeStamp { get; set; }
        [JsonPropertyName("callsign")] public string? Callsign { get; set; }
        [JsonPropertyName("activatorCallsign")] public string? ActivatorCallsign { get; set; }
        [JsonPropertyName("summitCode")] public string? SummitCode { get; set; }
        [JsonPropertyName("summitDetails")] public string? SummitDetails { get; set; }
        [JsonPropertyName("frequency")] public string? Frequency { get; set; }
        [JsonPropertyName("mode")] public string? Mode { get; set; }
        [JsonPropertyName("comments")] public string? Comments { get; set; }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Sota.Configured)
        {
            logger.LogInformation("SOTA feed is off; set HamAlert:Sota:Url and Enabled once SOTA has approved access");
            return;
        }

        var backoff = TimeSpan.FromSeconds(30);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Poll(stoppingToken);
                status.Connected(Name);
                backoff = TimeSpan.FromSeconds(30);
                await Task.Delay(TimeSpan.FromSeconds(options.Value.Sota.PollSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "SOTA poll failed, retrying in {Backoff}", backoff);
                status.Disconnected(Name, exception.Message);
                await Task.Delay(backoff, CancellationToken.None);
                backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
            }
        }
    }

    private async Task Poll(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"devanewbot ({options.Value.Callsign})");

        var spots = await client.GetFromJsonAsync<List<SotaSpot>>(options.Value.Sota.Url!, cancellationToken) ?? [];
        var watched = await watches.Snapshot(cancellationToken);
        var currentIds = new HashSet<long>();

        if (spots.Any(spot => spot.SummitCode == "DEPRECATED"))
        {
            throw new InvalidOperationException("SOTA reports this endpoint as deprecated; ask them for the current one.");
        }

        foreach (var spot in spots)
        {
            var spotted = spot.ActivatorCallsign ?? spot.Callsign;
            if (spot.Id is not { } id || spotted is null)
            {
                continue;
            }

            currentIds.Add(id);
            if (!seenSpotIds.Add(id))
            {
                continue;
            }

            var callsign = Callsigns.Base(spotted);
            if (!watched.ContainsKey(callsign) || FrequencyHz(spot.Frequency) is not { } frequencyHz)
            {
                continue;
            }

            await queue.Enqueue(new ReceivedSpot(
                Source: Name,
                Callsign: callsign,
                Reporter: Callsigns.Reporter(spot.Callsign ?? callsign),
                ReporterGrid: null,
                SpottedGrid: null,
                FrequencyHz: frequencyHz,
                Mode: spot.Mode?.ToUpperInvariant() ?? "?",
                Snr: null,
                Wpm: null,
                Comment: Comment(spot),
                HeardAt: spot.TimeStamp is { } time ? DateTime.SpecifyKind(time, DateTimeKind.Utc) : DateTime.UtcNow),
                cancellationToken);

            status.SpotReceived(Name);
        }

        seenSpotIds = currentIds.Count > 0 ? [.. seenSpotIds.Intersect(currentIds)] : [];
    }

    private static long? FrequencyHz(string? megaHertz) =>
        double.TryParse(megaHertz, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            ? (long)Math.Round(value * 1_000_000)
            : null;

    private static string? Comment(SotaSpot spot)
    {
        var parts = new[] { spot.SummitDetails ?? spot.SummitCode, spot.Comments?.Trim() }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0 ? null : string.Join(" · ", parts);
    }
}
