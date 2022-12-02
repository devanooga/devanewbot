namespace SlackDotNet;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlackDotNet.Options;
using SlackDotNet.Responses;
using SlackDotNet.Webhooks;
using WebSocketExtensions;

public class SlackSocket : IDisposable
{
    private SlackSocketOptions Options { get; set; }
    private ICommandService CommandService { get; set; }
    private WebSocketClient WebSocketClient { get; set; }
    private ILogger<SlackSocket> Logger { get; set; }
    private Dictionary<string, DateTime> MessageEnvelopes { get; set; } = new Dictionary<string, DateTime>();

    public SlackSocket(IOptions<SlackSocketOptions> options, ICommandService commandService, ILogger<SlackSocket> logger)
    {
        Options = options.Value;
        CommandService = commandService;
        Logger = logger;
    }

    public void Dispose()
    {
        WebSocketClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Requests that a Web Socket be opened, and connects.
    /// </summary>
    /// <returns></returns>
    public async Task Connect()
    {
        // First, call the `apps.connections.open` endpoint. This will give us a `wss` URL.
        var response = await "https://slack.com/api/apps.connections.open"
            .WithHeader("Authorization", $"Bearer {Options.AppToken}")
            .PostAsync()
            .ReceiveJson<Connection>();

        if (!response.Ok)
        {
            throw new Exception("Slack was unable to open a Web Socket");
        }

        WebSocketClient = new WebSocketClient()
        {
            MessageHandler = (e) => Task.Run(() => HandleMessageAsync(e.Data)),
        };

        await WebSocketClient.ConnectAsync(response.Url.ToString());
    }

    /// <summary>
    /// Handles a message from the WebSocket
    /// </summary>
    /// <param name="message"></param>
    private async Task HandleMessageAsync(string message)
    {
        var socketMessage = JsonNode.Parse(message);
        var envelopeId = (string)socketMessage["envelope_id"];

        if (!String.IsNullOrEmpty(envelopeId))
        {
            // Acknowledge that we've received this message
            await AcknowledgeMessage(envelopeId);

            // Check if we've already received this message. If so, return early.
            // Otherwise, add it to the dictionary of entities.
            if (MessageEnvelopes.ContainsKey(envelopeId))
            {
                return;
            }

            MessageEnvelopes.Add(envelopeId, DateTime.Now);
        }

        // Slack sends a "type" for each message. Based on that type, we need to perform a different action
        switch ((string)socketMessage["type"])
        {
            case "hello":
                // Received hello message. Just log and wave, boys. Log and wave.
                Logger.LogInformation("Received 'hello' ping from slack: " + message);
                break;
            case "slash_commands":
                // This is a command like `/spongebob whatever`.
                // Deserialize into a WebhookMessage to be handled
                Logger.LogInformation("Processing WebhookMessage: " + message);
                await CommandService.HandleMessage(
                    DeserializePayload<WebhookMessage>(socketMessage["payload"].AsObject()),
                    Options.CommandSuffix);
                break;
            case "interactive":
                Logger.LogInformation("Processing InteractiveMessage: " + message);
                await CommandService.HandleInteractive(
                    DeserializePayload<InteractiveMessage>(socketMessage["payload"].AsObject())
                );
                break;
            case "disconnect":
                // Slack will periodically ("once every few hours"), kill the connection.
                // We just need to open a new connection and keep on trucking.
                Logger.LogInformation("Slack Socket is disconnecting. Starting new connection.");
                await Connect();
                break;
            default:
                Logger.LogWarning("I don't know how to handle this message received from the Slack Socket: " + message);
                break;
        }

        // Make sure that the envelope cache doesn't grow bigger than 1 hour worth of messages
        ManageMessageEnvelopes();
    }

    /// <summary>
    /// Deserializes the Payload from a web socket message into type `T`
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="payload"></param>
    /// <returns></returns>
    private T DeserializePayload<T>(JsonObject payload)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        payload.WriteTo(writer);
        writer.Flush();

        JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);
        return JsonSerializer.Deserialize<T>(stream.ToArray(), serializerOptions);
    }

    /// <summary>
    /// This sends a message to Slack to acknowledge a message envelope.
    /// </summary>
    /// <param name="envelopeId"></param>
    /// <returns></returns>
    private async Task AcknowledgeMessage(string envelopeId)
    {
        await WebSocketClient.SendStringAsync("{\"envelope_id\": \"" + envelopeId + "\"}");
    }

    /// <summary>
    /// Removes message envelopes that are older than 1 hour.
    /// </summary>
    /// <returns></returns>
    private void ManageMessageEnvelopes()
    {
        var oneHour = new TimeSpan(1, 0, 0);
        var oneHourAgo = DateTime.Now - oneHour;
        foreach (var envelope in MessageEnvelopes)
        {
            if (DateTime.Compare(envelope.Value, oneHourAgo) < 0)
            {
                MessageEnvelopes.Remove(envelope.Key);
            }
        }
    }
}
