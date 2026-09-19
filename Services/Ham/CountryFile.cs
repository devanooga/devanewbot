namespace devanewbot.Services.Ham;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class CountryFile(IHttpClientFactory httpClientFactory, ILogger<CountryFile> logger)
{
    private const string SourceUrl = "https://www.country-files.com/cty/cty.dat";
    private static readonly char[] Modifiers = ['(', '[', '{', '<', '~'];
    private static readonly HashSet<string> PortableSuffixes = ["P", "M", "MM", "AM", "QRP", "A", "B", "R"];

    public record Entity(string Name, string Continent);

    private Dictionary<string, Entity> prefixes = new(StringComparer.Ordinal);
    private Dictionary<string, Entity> exactCallsigns = new(StringComparer.Ordinal);

    public bool Loaded => prefixes.Count > 0;

    public async Task Refresh(CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await httpClientFactory.CreateClient().GetStringAsync(SourceUrl, cancellationToken);
            var (parsedPrefixes, parsedExact) = Parse(content);
            prefixes = parsedPrefixes;
            exactCallsigns = parsedExact;
            logger.LogInformation("Loaded {Prefixes} prefixes from cty.dat", prefixes.Count);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not refresh cty.dat");
        }
    }

    public Entity? Lookup(string? callsign)
    {
        if (string.IsNullOrWhiteSpace(callsign) || !Loaded)
        {
            return null;
        }

        var upper = callsign.Trim().ToUpperInvariant();
        if (exactCallsigns.TryGetValue(upper, out var exact))
        {
            return exact;
        }

        var segments = upper.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(segment => !PortableSuffixes.Contains(segment) && !(segment.Length == 1 && char.IsDigit(segment[0])))
            .ToArray();

        return segments.Select(LongestPrefix).FirstOrDefault(entity => entity is not null);
    }

    private Entity? LongestPrefix(string segment)
    {
        for (var length = segment.Length; length > 0; length--)
        {
            if (prefixes.TryGetValue(segment[..length], out var entity))
            {
                return entity;
            }
        }

        return null;
    }

    private static (Dictionary<string, Entity>, Dictionary<string, Entity>) Parse(string content)
    {
        var parsedPrefixes = new Dictionary<string, Entity>(StringComparer.Ordinal);
        var parsedExact = new Dictionary<string, Entity>(StringComparer.Ordinal);

        foreach (var record in content.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = record.Split(':', 9);
            if (fields.Length < 9)
            {
                continue;
            }

            var entity = new Entity(fields[0].Trim(), fields[3].Trim());
            var tokens = fields[8].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var rawToken in tokens.Prepend(fields[7].Trim().TrimStart('*')))
            {
                var exact = rawToken.StartsWith('=');
                var token = rawToken.TrimStart('=');
                var modifier = token.IndexOfAny(Modifiers);
                if (modifier >= 0)
                {
                    token = token[..modifier];
                }

                if (token.Length == 0)
                {
                    continue;
                }

                (exact ? parsedExact : parsedPrefixes).TryAdd(token, entity);
            }
        }

        return (parsedPrefixes, parsedExact);
    }
}
