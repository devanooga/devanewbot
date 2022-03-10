namespace SlackDotNet.Webhooks
{
    using Newtonsoft.Json;

    public class InteractiveAttribute
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
