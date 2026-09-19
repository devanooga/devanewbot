namespace devanewbot.Models;

using System;
using System.Text.RegularExpressions;

public static partial class Maidenhead
{
    private const double EarthRadiusKm = 6371.0;

    [GeneratedRegex("^[A-R]{2}[0-9]{2}(?:[A-X]{2}(?:[0-9]{2})?)?$", RegexOptions.IgnoreCase)]
    private static partial Regex GridPattern { get; }

    public static bool IsValid(string? grid) => grid is not null && GridPattern.IsMatch(grid);

    public static string? Normalize(string? grid)
    {
        if (!IsValid(grid))
        {
            return null;
        }

        var upper = grid!.ToUpperInvariant();
        return upper.Length > 4 ? upper[..4] + upper[4..6].ToLowerInvariant() + upper[6..] : upper;
    }

    public static (double Latitude, double Longitude)? Center(string? grid)
    {
        var normalized = Normalize(grid);
        if (normalized is null)
        {
            return null;
        }

        var upper = normalized.ToUpperInvariant();
        var longitude = (upper[0] - 'A') * 20.0 - 180.0;
        var latitude = (upper[1] - 'A') * 10.0 - 90.0;
        longitude += (upper[2] - '0') * 2.0;
        latitude += (upper[3] - '0') * 1.0;
        var cellWidth = 2.0;
        var cellHeight = 1.0;

        if (upper.Length >= 6)
        {
            cellWidth = 2.0 / 24;
            cellHeight = 1.0 / 24;
            longitude += (upper[4] - 'A') * cellWidth;
            latitude += (upper[5] - 'A') * cellHeight;
        }

        if (upper.Length >= 8)
        {
            cellWidth /= 10;
            cellHeight /= 10;
            longitude += (upper[6] - '0') * cellWidth;
            latitude += (upper[7] - '0') * cellHeight;
        }

        return (latitude + cellHeight / 2, longitude + cellWidth / 2);
    }

    public static double? DistanceKm(string? fromGrid, string? toGrid)
    {
        if (Center(fromGrid) is not { } from || Center(toGrid) is not { } to)
        {
            return null;
        }

        var latitudeDelta = Radians(to.Latitude - from.Latitude);
        var longitudeDelta = Radians(to.Longitude - from.Longitude);
        var a = Math.Sin(latitudeDelta / 2) * Math.Sin(latitudeDelta / 2)
            + Math.Cos(Radians(from.Latitude)) * Math.Cos(Radians(to.Latitude))
            * Math.Sin(longitudeDelta / 2) * Math.Sin(longitudeDelta / 2);

        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double Radians(double degrees) => degrees * Math.PI / 180.0;
}
