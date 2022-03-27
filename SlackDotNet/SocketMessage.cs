namespace SlackDotNet;

using SlackDotNet.Webhooks;

public class SocketMessage
{
    public string EnvelopeId { get; set; }
    public WebhookMessage Payload { get; set; }
    public string Type { get; set; }
    public bool AcceptsResponsePayload { get; set; }
}
