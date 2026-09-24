namespace devanewbot.Services.Ham;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using devanewbot.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;

public class PskReporterFeed(
    HamSpotQueue queue,
    HamWatchCache watches,
    HamFeedStatus status,
    IOptions<HamAlertOptions> options,
    ILogger<PskReporterFeed> logger) : BackgroundService
{
    public const string Name = "PSK Reporter";

    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    private readonly HashSet<string> subscribed = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = new MqttClientFactory().CreateMqttClient();
        client.ApplicationMessageReceivedAsync += args => Handle(args, stoppingToken);
        client.DisconnectedAsync += args =>
        {
            subscribed.Clear();
            status.Disconnected(Name, args.ReasonString ?? args.Exception?.Message);
            return Task.CompletedTask;
        };

        var clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(options.Value.PskReporterHost, options.Value.PskReporterPort)
            .WithClientId($"devanewbot-{options.Value.Callsign}")
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
            .Build();

        var backoff = TimeSpan.FromSeconds(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(clientOptions, stoppingToken);
                    status.Connected(Name);
                    backoff = TimeSpan.FromSeconds(5);
                }

                await SyncSubscriptions(client, stoppingToken);
                await Task.Delay(SyncInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "PSK Reporter feed failed, retrying in {Backoff}", backoff);
                status.Disconnected(Name, exception.Message);
                await Task.Delay(backoff, CancellationToken.None);
                backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
            }
        }

        if (client.IsConnected)
        {
            await client.DisconnectAsync();
        }
    }

    private async Task SyncSubscriptions(IMqttClient client, CancellationToken cancellationToken)
    {
        var wanted = (await watches.Snapshot(cancellationToken)).Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = wanted.Except(subscribed).ToArray();
        var removed = subscribed.Except(wanted).ToArray();

        if (added.Length > 0)
        {
            var subscribe = new MqttClientSubscribeOptionsBuilder();
            foreach (var callsign in added)
            {
                subscribe.WithTopicFilter(Topic(callsign));
            }

            await client.SubscribeAsync(subscribe.Build(), cancellationToken);
            subscribed.UnionWith(added);
            logger.LogInformation("PSK Reporter now watching {Callsigns}", string.Join(", ", added));
        }

        if (removed.Length > 0)
        {
            var unsubscribe = new MqttClientUnsubscribeOptionsBuilder();
            foreach (var callsign in removed)
            {
                unsubscribe.WithTopicFilter(Topic(callsign));
            }

            await client.UnsubscribeAsync(unsubscribe.Build(), cancellationToken);
            subscribed.ExceptWith(removed);
        }
    }

    private static string Topic(string callsign) => $"pskr/filter/v2/+/+/{callsign.ToUpperInvariant()}/#";

    private async Task Handle(MqttApplicationMessageReceivedEventArgs args, CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonNode.Parse(args.ApplicationMessage.ConvertPayloadToString());
            if (payload is null)
            {
                return;
            }

            var sender = payload["sc"]?.GetValue<string>();
            var reporter = payload["rc"]?.GetValue<string>();
            var frequency = payload["f"]?.GetValue<long>();
            if (sender is null || reporter is null || frequency is null)
            {
                return;
            }

            var heardAt = payload["t"] is { } time
                ? DateTimeOffset.FromUnixTimeSeconds(time.GetValue<long>()).UtcDateTime
                : DateTime.UtcNow;

            await queue.Enqueue(new ReceivedSpot(
                Source: Name,
                Callsign: Callsigns.Base(sender),
                Reporter: Callsigns.Reporter(reporter),
                ReporterGrid: payload["rl"]?.GetValue<string>(),
                SpottedGrid: payload["sl"]?.GetValue<string>(),
                FrequencyHz: frequency.Value,
                Mode: payload["md"]?.GetValue<string>()?.ToUpperInvariant() ?? "?",
                Snr: payload["rp"]?.GetValue<int>(),
                Wpm: null,
                Comment: null,
                HeardAt: heardAt), cancellationToken);

            status.SpotReceived(Name);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not parse PSK Reporter message on {Topic}", args.ApplicationMessage.Topic);
        }
    }
}
