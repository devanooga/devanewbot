namespace devanewbot.Models;

using System;
using System.Linq;

public static class Callsigns
{
    public static string Base(string callsign)
    {
        var segments = callsign.Trim().ToUpperInvariant().Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 ? string.Empty : segments.MaxBy(segment => segment.Length)!;
    }

    public static string Reporter(string reporter)
    {
        var trimmed = reporter.Trim().TrimEnd(':').ToUpperInvariant();
        if (trimmed.EndsWith("-#"))
        {
            trimmed = trimmed[..^2];
        }

        var dash = trimmed.IndexOf('-');
        return dash > 0 ? trimmed[..dash] : trimmed;
    }
}
