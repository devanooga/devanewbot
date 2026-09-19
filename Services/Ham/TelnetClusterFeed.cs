namespace devanewbot.Services.Ham;

using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using devanewbot.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public partial class TelnetClusterFeed(
    HamSpotQueue queue,
    HamWatchCache watches,
    HamFeedStatus status,
    IOptions<HamAlertOptions> options,
    ILogger<TelnetClusterFeed> logger) : BackgroundService
{
    private static readonly TimeSpan LoginTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan SilenceTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);
    private static readonly string[] ModeWords = ["CW", "SSB", "USB", "LSB", "FT8", "FT4", "RTTY", "PSK31", "PSK", "JS8", "AM", "FM", "DIGI", "MSK144", "Q65", "JT65", "WSPR"];

    [GeneratedRegex(@"^DX de (?<reporter>[A-Z0-9/\-#]+?):?\s+(?<frequency>\d+(?:\.\d+)?)\s+(?<callsign>[A-Z0-9/]+)\s*(?<comment>.*?)\s*(?<time>\d{4})Z", RegexOptions.IgnoreCase)]
    private static partial Regex SpotLine { get; }

    [GeneratedRegex(@"^(?<mode>[A-Z0-9]+)\s+(?<snr>-?\d+)\s*dB(?:\s+(?<speed>\d+)\s*(?:WPM|BPS))?\s*(?<rest>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex SkimmerComment { get; }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(options.Value.TelnetNodes.Select(node => RunNode(node, stoppingToken)));

    private async Task RunNode(HamAlertOptions.TelnetNode node, CancellationToken stoppingToken)
    {
        var backoff = TimeSpan.FromSeconds(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReadNode(node, stoppingToken);
                backoff = TimeSpan.FromSeconds(5);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "{Node} feed failed, retrying in {Backoff}", node.Name, backoff);
                status.Disconnected(node.Name, exception.Message);
                await Task.Delay(backoff, CancellationToken.None);
                backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
            }
        }
    }

    private async Task ReadNode(HamAlertOptions.TelnetNode node, CancellationToken stoppingToken)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(node.Host, node.Port, stoppingToken);
        using var stream = tcp.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true, NewLine = "\r\n" };

        await WaitForLoginPrompt(reader, stoppingToken);
        await writer.WriteLineAsync(options.Value.Callsign);
        status.Connected(node.Name);
        logger.LogInformation("Connected to {Node} at {Host}:{Port} as {Callsign}", node.Name, node.Host, node.Port, options.Value.Callsign);

        while (!stoppingToken.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(stoppingToken).AsTask().WaitAsync(SilenceTimeout, stoppingToken);
            }
            catch (TimeoutException)
            {
                throw new IOException($"No data from {node.Name} for {SilenceTimeout.TotalMinutes} minutes");
            }

            if (line is null)
            {
                throw new IOException($"{node.Name} closed the connection");
            }

            await HandleLine(node, line, stoppingToken);
        }
    }

    private static async Task WaitForLoginPrompt(StreamReader reader, CancellationToken cancellationToken)
    {
        var buffer = new char[512];
        var seen = new StringBuilder();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(LoginTimeout);

        while (true)
        {
            var read = await reader.ReadAsync(buffer, timeout.Token);
            if (read == 0)
            {
                throw new IOException("Connection closed before the login prompt");
            }

            seen.Append(buffer, 0, read);
            var text = seen.ToString();
            if (text.Contains("call", StringComparison.OrdinalIgnoreCase) || text.Contains("login", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }
    }

    private async Task HandleLine(HamAlertOptions.TelnetNode node, string line, CancellationToken cancellationToken)
    {
        var match = SpotLine.Match(line);
        if (!match.Success)
        {
            return;
        }

        var fullCallsign = match.Groups["callsign"].Value.ToUpperInvariant();
        var callsign = Callsigns.Base(fullCallsign);
        var watched = await watches.Snapshot(cancellationToken);
        if (!watched.ContainsKey(callsign))
        {
            return;
        }

        var frequencyHz = (long)Math.Round(double.Parse(match.Groups["frequency"].Value, System.Globalization.CultureInfo.InvariantCulture) * 1000);
        var comment = match.Groups["comment"].Value.Trim();
        var (mode, snr, speed, rest) = ParseComment(comment);
        var notes = string.Join(" ", new[] { fullCallsign != callsign ? $"as {fullCallsign}" : null, rest }.Where(part => !string.IsNullOrWhiteSpace(part)));

        await queue.Enqueue(new ReceivedSpot(
            Source: node.Name,
            Callsign: callsign,
            Reporter: Callsigns.Reporter(match.Groups["reporter"].Value),
            ReporterGrid: null,
            SpottedGrid: null,
            FrequencyHz: frequencyHz,
            Mode: mode,
            Snr: snr,
            Wpm: speed,
            Comment: notes.Length > 0 ? notes : null,
            HeardAt: HeardAt(match.Groups["time"].Value)), cancellationToken);

        status.SpotReceived(node.Name);
    }

    private static (string Mode, int? Snr, int? Speed, string Notes) ParseComment(string comment)
    {
        var skimmer = SkimmerComment.Match(comment);
        if (skimmer.Success)
        {
            return (
                skimmer.Groups["mode"].Value.ToUpperInvariant(),
                int.Parse(skimmer.Groups["snr"].Value),
                skimmer.Groups["speed"].Success ? int.Parse(skimmer.Groups["speed"].Value) : null,
                skimmer.Groups["rest"].Value.Trim());
        }

        var words = comment.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var mode = words.FirstOrDefault(word => ModeWords.Contains(word.ToUpperInvariant()))?.ToUpperInvariant() ?? "?";
        return (mode, null, null, comment);
    }

    private static DateTime HeardAt(string hhmm)
    {
        var now = DateTime.UtcNow;
        var heard = new DateTime(now.Year, now.Month, now.Day, int.Parse(hhmm[..2]), int.Parse(hhmm[2..]), 0, DateTimeKind.Utc);
        return heard > now.AddMinutes(5) ? heard.AddDays(-1) : heard;
    }
}
