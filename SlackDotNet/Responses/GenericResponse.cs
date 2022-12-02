namespace SlackDotNet.Responses;

using Newtonsoft.Json;

public class GenericResponse
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }
}
