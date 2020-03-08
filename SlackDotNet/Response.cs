using Newtonsoft.Json;
using SlackDotNet.Responses;

namespace SlackDotNet
{
    public class Response
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }
    }
}