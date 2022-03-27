namespace SlackDotNet.Webhooks;
using System;
using System.Text.Json.Serialization;

public class WebhookMessage
{
    public string Token { get; set; }

    [JsonPropertyName("team_id")]
    public string TeamId { get; set; }

    [JsonPropertyName("team_domain")]
    public string TeamDomain { get; set; }

    [JsonPropertyName("channel_id")]
    public string ChannelId { get; set; }

    [JsonPropertyName("channel_name")]
    public string ChannelName { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; }

    public string Command { get; set; }

    public string Text { get; set; }

    [JsonPropertyName("api_app_id")]
    public string ApiAppId { get; set; }

    [JsonPropertyName("is_enterprise_install")]
    public string IsEnterpriseInstall { get; set; }

    [JsonPropertyName("response_url")]
    public Uri ResponseUrl { get; set; }

    [JsonPropertyName("trigger_id")]
    public string TriggerId { get; set; }
}
