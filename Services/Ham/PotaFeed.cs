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

public class PotaFeed(
    HamSpotQueue queue,
    HamWatchCache watches,
    HamFeedStatus status,
    IHttpClientFactory httpClientFactory,
    IOptions<HamAlertOptions> options,
    ILogger<PotaFeed> logger) : BackgroundService
{
    public const string Name = "POTA";

    private const string SpotsUrl = "https://api.pota.app/spot/activator";
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    private HashSet<long> seenSpotIds = [];

    private class PotaSpot
    {
        [JsonPropertyName("spotId")] public long SpotId { get; set; }
        [JsonPropertyName("activator")] public string? Activator { get; set; }
        [JsonPropertyName("spotter")] public string? Spotter { get; set; }
        [JsonPropertyName("frequency")] public string? Frequency { get; set; }
        [JsonPropertyName("mode")] public string? Mode { get; set; }
        [JsonPropertyName("reference")] public string? Reference { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("locationDesc")] public string? LocationDescription { get; set; }
        [JsonPropertyName("comments")] public string? Comments { get; set; }
        [JsonPropertyName("grid4")] public string? Grid4 { get; set; }
        [JsonPropertyName("grid6")] public string? Grid6 { get; set; }
        [JsonPropertyName("spotTime")] public DateTime? SpotTime { get; set; }
        [JsonPropertyName("invalid")] public bool? Invalid { get; set; }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Pota.Enabled)
        {
            logger.LogInformation("POTA feed is disabled");
            return;
        }

        var backoff = TimeSpan.FromSeconds(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Poll(stoppingToken);
                status.Connected(Name);
                backoff = TimeSpan.FromSeconds(5);
                await Task.Delay(TimeSpan.FromSeconds(options.Value.Pota.PollSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "POTA poll failed, retrying in {Backoff}", backoff);
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

        var spots = await client.GetFromJsonAsync<List<PotaSpot>>(SpotsUrl, cancellationToken) ?? [];
        var watched = await watches.Snapshot(cancellationToken);
        var currentIds = new HashSet<long>();

        foreach (var spot in spots)
        {
            currentIds.Add(spot.SpotId);

            if (spot.Invalid == true || spot.Activator is null || !seenSpotIds.Add(spot.SpotId))
            {
                continue;
            }

            var callsign = Callsigns.Base(spot.Activator);
            if (!watched.ContainsKey(callsign) || FrequencyHz(spot.Frequency) is not { } frequencyHz)
            {
                continue;
            }

            await queue.Enqueue(new ReceivedSpot(
                Source: Name,
                Callsign: callsign,
                Reporter: Callsigns.Reporter(spot.Spotter ?? callsign),
                ReporterGrid: null,
                SpottedGrid: Maidenhead.Normalize(spot.Grid6) ?? Maidenhead.Normalize(spot.Grid4),
                FrequencyHz: frequencyHz,
                Mode: spot.Mode?.ToUpperInvariant() ?? "?",
                Snr: null,
                Wpm: null,
                Comment: Comment(spot),
                HeardAt: spot.SpotTime is { } time ? DateTime.SpecifyKind(time, DateTimeKind.Utc) : DateTime.UtcNow),
                cancellationToken);

            status.SpotReceived(Name);
        }

        seenSpotIds = currentIds.Count > 0 ? [.. seenSpotIds.Intersect(currentIds)] : [];
    }

    private static long? FrequencyHz(string? kiloHertz) =>
        double.TryParse(kiloHertz, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            ? (long)Math.Round(value * 1000)
            : null;

    private static string? Comment(PotaSpot spot)
    {
        var park = new[] { spot.Reference, spot.Name, spot.LocationDescription }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        var parts = new[] { string.Join(" ", park), spot.Comments?.Trim() }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0 ? null : string.Join(" · ", parts);
    }
}
