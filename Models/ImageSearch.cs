namespace devanewbot.Models;

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ImageSearch
{
    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("instrumentation")]
    public object Instrumentation { get; set; }

    [JsonProperty("webSearchUrl")]
    public Uri WebSearchUrl { get; set; }

    [JsonProperty("totalEstimatedMatches")]
    public long TotalEstimatedMatches { get; set; }

    [JsonProperty("value")]
    public List<ImageSearchValue> Value { get; set; }

    [JsonProperty("queryExpansions")]
    public List<object> QueryExpansions { get; set; }

    [JsonProperty("pivotSuggestions")]
    public List<object> PivotSuggestions { get; set; }
}
