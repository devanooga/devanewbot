namespace SlackDotNet.Webhooks;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SlackDotNet.Payloads;

public class InteractiveMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("actions")]
    public List<ChatAction> Actions { get; set; }

    [JsonPropertyName("callback_id")]
    public string CallbackId { get; set; }

    [JsonPropertyName("team")]
    public InteractiveAttribute Team { get; set; }

    [JsonPropertyName("channel")]
    public InteractiveAttribute Channel { get; set; }

    [JsonPropertyName("user")]
    public InteractiveAttribute User { get; set; }

    [JsonPropertyName("action_ts")]
    public string ActionTs { get; set; }

    [JsonPropertyName("message_ts")]
    public string MessageTs { get; set; }

    [JsonPropertyName("attachment_id")]
    public string AttachmentId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonPropertyName("is_app_unfurl")]
    public bool IsAppUnfurl { get; set; }

    [JsonPropertyName("response_url")]
    public Uri ResponseUrl { get; set; }

    [JsonPropertyName("trigger_id")]
    public string TriggerId { get; set; }
}
