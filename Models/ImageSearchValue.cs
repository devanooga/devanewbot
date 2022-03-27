namespace devanewbot.Models;

using System;
using Newtonsoft.Json;

public class ImageSearchValue
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("datePublished")]
    public DateTimeOffset DatePublished { get; set; }

    [JsonProperty("contentSize")]
    public string ContentSize { get; set; }

    [JsonProperty("hostPageDisplayUrl")]
    public string HostPageDisplayUrl { get; set; }

    [JsonProperty("width")]
    public long Width { get; set; }

    [JsonProperty("height")]
    public long Height { get; set; }

    [JsonProperty("thumbnail")]
    public object Thumbnail { get; set; }

    [JsonProperty("imageInsightsToken")]
    public string ImageInsightsToken { get; set; }

    [JsonProperty("imageId")]
    public string ImageId { get; set; }

    [JsonProperty("accentColor")]
    public string AccentColor { get; set; }

    [JsonProperty("webSearchUrl")]
    public Uri WebSearchUrl { get; set; }

    [JsonProperty("thumbnailUrl")]
    public Uri ThumbnailUrl { get; set; }

    [JsonProperty("encodingFormat")]
    public string EncodingFormat { get; set; }

    [JsonProperty("contentUrl")]
    public Uri ContentUrl { get; set; }
}
