using Newtonsoft.Json;

namespace SlackDotNet
{
    public class Payload
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }
}