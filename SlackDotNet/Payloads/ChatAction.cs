namespace SlackDotNet.Payloads;

using Newtonsoft.Json;

public class ChatAction
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("style")]
    public string Style { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }
}
