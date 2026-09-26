namespace devanewbot.Seeders;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using devanewbot.Data;
using devanewbot.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public partial class ModerationHistorySeeder(DevanewbotContext db, ILogger<ModerationHistorySeeder> logger) : ISeeder
{
    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2})$")]
    private static partial Regex DatePattern { get; }

    [GeneratedRegex(@"^(\d{1,2}):(\d{2})\s*([AP]M)?\s*(E[SD]T)?$", RegexOptions.IgnoreCase)]
    private static partial Regex TimePattern { get; }

    public async Task Seed(CancellationToken cancellationToken = default)
    {
        if (await db.ModerationActions.AnyAsync(action => action.Source == ModerationActionSource.Imported, cancellationToken))
        {
            return;
        }

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("administrative-actions.md")!;
        using var reader = new StreamReader(stream);
        var actions = Parse(await reader.ReadToEndAsync(cancellationToken)).ToList();

        db.ModerationActions.AddRange(actions);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Imported {Count} moderation actions from administrative-actions.md", actions.Count);
    }

    private static IEnumerable<ModerationAction> Parse(string markdown)
    {
        foreach (var line in markdown.Split('\n'))
        {
            var columns = line.Split('|').Select(column => column.Trim()).ToArray();
            if (columns.Length < 5 || !DatePattern.IsMatch(columns[0]))
            {
                continue;
            }

            var action = columns[2];
            yield return new ModerationAction
            {
                OccurredAt = OccurredAt(columns[0], columns[1]),
                Kind = Kind(action),
                Source = ModerationActionSource.Imported,
                Action = action,
                // A few rows have an extra column between reason and administrator.
                Reason = string.Join("; ", columns[3..^1].Where(column => column.Length > 0)),
                Administrator = columns[^1]
            };
        }
    }

    private static DateTime OccurredAt(string date, string time)
    {
        var day = DateTime.ParseExact(date, "yyyy-MM-dd", null);
        var match = TimePattern.Match(time);
        if (!match.Success)
        {
            return DateTime.SpecifyKind(day.AddHours(5), DateTimeKind.Utc);
        }

        var hour = int.Parse(match.Groups[1].Value);
        var meridiem = match.Groups[3].Value.ToUpperInvariant();
        if (hour < 12 && meridiem == "PM")
        {
            hour += 12;
        }
        else if (hour == 12 && meridiem == "AM")
        {
            hour = 0;
        }

        var offsetHours = match.Groups[4].Value.Equals("EDT", StringComparison.OrdinalIgnoreCase) ? 4 : 5;
        var local = day.AddHours(hour).AddMinutes(int.Parse(match.Groups[2].Value));
        return DateTime.SpecifyKind(local.AddHours(offsetHours), DateTimeKind.Utc);
    }

    private static ModerationActionKind Kind(string action) =>
        action.StartsWith("Deactivat", StringComparison.OrdinalIgnoreCase) ? ModerationActionKind.Deactivated
        : action.Contains("channel ban", StringComparison.OrdinalIgnoreCase) ? ModerationActionKind.ChannelBan
        : action.StartsWith("Remove", StringComparison.OrdinalIgnoreCase) && action.Contains("post", StringComparison.OrdinalIgnoreCase) ? ModerationActionKind.RemovedMessage
        : ModerationActionKind.Other;
}
