namespace SlackDotNet;

using Newtonsoft.Json;

public class Payload
{
    [JsonProperty("token")]
    public string Token { get; set; }
}
