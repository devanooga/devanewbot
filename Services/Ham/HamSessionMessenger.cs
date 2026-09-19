namespace devanewbot.Services.Ham;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.WebApi;

public class HamSessionMessenger(ISlackApiClient slack, IOptions<HamAlertOptions> options)
{
    public async Task<(string ChannelId, string Ts)> Post(HamSessionView view)
    {
        if (!options.Value.ChannelConfigured)
        {
            throw new InvalidOperationException("HamAlert:ChannelId is not set, so there is nowhere to announce this.");
        }

        var response = await slack.Chat.PostMessage(new Message
        {
            Channel = options.Value.ChannelId,
            Text = Fallback(view),
            Blocks = Blocks(view)
        });

        return (response.Channel, response.Ts);
    }

    public Task Update(string channelId, string ts, HamSessionView view) =>
        slack.Chat.Update(new MessageUpdate
        {
            ChannelId = channelId,
            Ts = ts,
            Text = Fallback(view),
            Blocks = Blocks(view)
        });

    public static string Fallback(HamSessionView view)
    {
        var furthest = view.Furthest?.DistanceKm is { } km ? $", furthest {Km(km)} ({view.Furthest.Callsign})" : string.Empty;
        var verb = view.State == HamSessionState.Closed ? "was" : "is";
        return $"{view.Callsign} {verb} on the air on {BandMode(view)}: {view.SpotCount} spots{furthest}";
    }

    public static IList<Block> Blocks(HamSessionView view)
    {
        var who = view.SlackUserId is null ? $"`{view.Callsign}`" : $"<@{view.SlackUserId}> (`{view.Callsign}`)";
        var verb = view.State switch
        {
            HamSessionState.Closed => "was on the air",
            HamSessionState.Quiet => "was last heard on the air",
            _ => "is on the air"
        };
        var where = view.Grid is null ? string.Empty : $" from *{view.Grid}*";
        var frequency = view.FrequencyHz is { } hz ? $" · {hz / 1_000_000.0:0.000} MHz" : string.Empty;

        var blocks = new List<Block>
        {
            new HeaderBlock { Text = new PlainText($"{Emoji(view)} {view.Callsign} on {BandMode(view)}") },
            new SectionBlock { Text = new Markdown($"{who} {verb}{where}{frequency}") },
            new SectionBlock
            {
                Fields =
                [
                    new Markdown($"*Spots*\n{view.SpotCount:N0}"),
                    new Markdown($"*Reporters*\n{view.ReporterCount:N0}"),
                    new Markdown($"*Furthest*\n{Describe(view.Furthest, reporter => reporter.DistanceKm is { } km ? Km(km) : null)}"),
                    new Markdown($"*Best signal*\n{Describe(view.Loudest, reporter => reporter.Snr is { } snr ? $"{snr:+#;-#;0} dB" : null)}"),
                    new Markdown($"*First heard*\n{SlackTime(view.OpenedAt)}"),
                    new Markdown($"*Last heard*\n{SlackTime(view.LastHeardAt)}")
                ]
            }
        };

        if (view.Countries.Count > 0)
        {
            var countries = string.Join(" · ", view.Countries.Select(country => $"{country.Country} ×{country.Reporters}"));
            blocks.Add(new ContextBlock { Elements = [new Markdown($":earth_americas: {countries}")] });
        }

        if (view.Comment is not null)
        {
            blocks.Add(new ContextBlock { Elements = [new Markdown($":memo: {view.Comment}")] });
        }

        var sources = view.Sources.Count > 0 ? $" · via {string.Join(", ", view.Sources)}" : string.Empty;
        // Slack marks its own edits and the fields above carry the timestamps, so this line only
        // says what they cannot: whether the operator is still there.
        var state = view.State switch
        {
            HamSessionState.Closed =>
                $":white_circle: QRT after {Duration(view.ClosedAt!.Value - view.OpenedAt)} on the air{sources}",
            HamSessionState.Quiet =>
                $":large_yellow_circle: Nothing heard for {Duration(DateTime.UtcNow - view.LastHeardAt)}, likely QRT{sources}",
            _ =>
                $":large_green_circle: Live{sources}"
        };
        blocks.Add(new ContextBlock { Elements = [new Markdown(state)] });

        return blocks;
    }

    private static string Emoji(HamSessionView view) =>
        view.State == HamSessionState.Closed ? ":radio:" : ":satellite_antenna:";

    private static string BandMode(HamSessionView view) => view.Mode == "?" ? view.Band : $"{view.Band} {view.Mode}";

    private static string Describe(HamSessionView.Reporter? reporter, Func<HamSessionView.Reporter, string?> measure)
    {
        if (reporter is null || measure(reporter) is not { } value)
        {
            return "not yet";
        }

        var country = reporter.Country is null ? string.Empty : $" · {reporter.Country}";
        return $"{value} · `{reporter.Callsign}`{country}";
    }

    private const double MilesPerKilometre = 0.621371;

    private static string Km(double km) =>
        $"{km.ToString("N0", CultureInfo.InvariantCulture)} km / {(km * MilesPerKilometre).ToString("N0", CultureInfo.InvariantCulture)} mi";

    private static string SlackTime(DateTime utc)
    {
        var unix = new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds();
        return $"<!date^{unix}^{{time}}|{utc:HH:mm} UTC>";
    }

    private static string Duration(TimeSpan span) =>
        span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes:00}m" : $"{Math.Max(1, (int)span.TotalMinutes)}m";
}
