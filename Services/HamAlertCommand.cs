namespace devanewbot.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using devanewbot.Data.Models;
using devanewbot.Models;
using devanewbot.Services.Ham;
using Microsoft.Extensions.Logging;
using SlackNet.Interaction;
using SlackNet.WebApi;

public partial class HamAlertCommand(HamWatchService watchList, HamFeedStatus feeds, ILogger<HamAlertCommand> logger) : ISlashCommandHandler
{
    private const string Usage = "Usage: `/hamalert list`, `/hamalert add W1AW [K4XYZ ...] [EM75]`, `/hamalert remove W1AW`, `/hamalert grid W1AW EM75`, `/hamalert claim W1AW`, `/hamalert status`";

    [GeneratedRegex("^[A-Z0-9]{3,12}$")]
    private static partial Regex CallsignPattern { get; }

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        var arguments = (command.Text ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(argument => argument.ToUpperInvariant())
            .ToArray();
        var action = arguments.FirstOrDefault()?.ToLowerInvariant() ?? "list";
        var operands = arguments.Skip(1).ToArray();

        try
        {
            return action switch
            {
                "list" => Ephemeral(await Describe()),
                "status" => Ephemeral(Status()),
                "add" => await Add(command.UserId, operands),
                "remove" or "rm" or "delete" => await Remove(command.UserId, operands),
                "grid" => await Grid(operands),
                "claim" => await Claim(command.UserId, operands),
                _ => Ephemeral(Usage)
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "HamAlert command failed");
            return Ephemeral($":warning: {exception.Message}");
        }
    }

    private async Task<SlashCommandResponse> Add(string userId, string[] operands)
    {
        var grid = operands.Length > 1 && Maidenhead.IsValid(operands[^1]) ? operands[^1] : null;
        var callsigns = grid is null ? operands : operands[..^1];

        if (callsigns.Length == 0)
        {
            return Ephemeral(Usage);
        }

        if (Invalid(callsigns) is { } invalid)
        {
            return invalid;
        }

        var added = await watchList.Add(callsigns, grid, userId);
        return Announce(userId, "added", added, callsigns, "to", "already on");
    }

    private async Task<SlashCommandResponse> Remove(string userId, string[] callsigns)
    {
        if (callsigns.Length == 0)
        {
            return Ephemeral(Usage);
        }

        if (Invalid(callsigns) is { } invalid)
        {
            return invalid;
        }

        var removed = await watchList.Remove(callsigns);
        return Announce(userId, "removed", removed, callsigns, "from", "not on");
    }

    private async Task<SlashCommandResponse> Grid(string[] operands)
    {
        if (operands.Length != 2 || !CallsignPattern.IsMatch(operands[0]) || !Maidenhead.IsValid(operands[1]))
        {
            return Ephemeral(Usage);
        }

        return await watchList.SetGrid(operands[0], operands[1])
            ? Ephemeral($"`{operands[0]}` is now located at `{Maidenhead.Normalize(operands[1])}`.")
            : Ephemeral($"`{operands[0]}` is not on the watch list.");
    }

    private async Task<SlashCommandResponse> Claim(string userId, string[] operands)
    {
        if (operands.Length != 1 || !CallsignPattern.IsMatch(operands[0]))
        {
            return Ephemeral(Usage);
        }

        return await watchList.Claim(operands[0], userId)
            ? Ephemeral($"Alerts for `{operands[0]}` will now mention you.")
            : Ephemeral($"`{operands[0]}` is not on the watch list.");
    }

    private async Task<string> Describe()
    {
        var watches = await watchList.List();

        return watches.Count == 0
            ? "The ham alert watch list is empty."
            : $"Watching {watches.Count}: {string.Join(", ", watches.Select(Describe))}";
    }

    private static string Describe(HamWatch watch)
    {
        var details = new[] { watch.Grid, watch.SlackUserId is null ? null : $"<@{watch.SlackUserId}>" }
            .Where(detail => detail is not null);
        var suffix = string.Join(" ", details);
        return suffix.Length == 0 ? $"`{watch.Callsign}`" : $"`{watch.Callsign}` ({suffix})";
    }

    private string Status()
    {
        var snapshot = feeds.Snapshot();
        if (snapshot.Count == 0)
        {
            return "No feeds have reported in yet.";
        }

        return string.Join("\n", snapshot.Select(feed =>
        {
            var state = feed.Connected ? ":large_green_circle: connected" : $":red_circle: down{(feed.LastError is null ? string.Empty : $" ({feed.LastError})")}";
            var lastSpot = feed.LastSpotAt is { } at ? $", last spot {Ago(at)}" : ", no spots yet";
            return $"*{feed.Name}*: {state}{lastSpot}";
        }));
    }

    private static string Ago(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        return span.TotalMinutes < 1 ? "just now"
            : span.TotalHours < 1 ? $"{(int)span.TotalMinutes}m ago"
            : $"{(int)span.TotalHours}h {span.Minutes}m ago";
    }

    private static SlashCommandResponse? Invalid(string[] callsigns)
    {
        var invalid = callsigns.Where(callsign => !CallsignPattern.IsMatch(callsign)).ToArray();
        return invalid.Length > 0 ? Ephemeral($"Not a callsign: {Format(invalid)}") : null;
    }

    private static SlashCommandResponse Announce(string userId, string verb, string[] changed, string[] requested, string preposition, string skippedReason)
    {
        var skipped = requested.Except(changed, StringComparer.OrdinalIgnoreCase).ToArray();
        var lines = new List<string>();

        if (changed.Length > 0)
        {
            lines.Add($"<@{userId}> {verb} {Format(changed)} {preposition} the ham alert watch list.");
        }

        if (skipped.Length > 0)
        {
            lines.Add($"{Format(skipped)} {(skipped.Length == 1 ? "was" : "were")} {skippedReason} the list already.");
        }

        return new SlashCommandResponse
        {
            ResponseType = changed.Length > 0 ? ResponseType.InChannel : ResponseType.Ephemeral,
            Message = new Message { Text = string.Join("\n", lines) }
        };
    }

    private static string Format(IEnumerable<string> callsigns) =>
        string.Join(", ", callsigns.Select(callsign => $"`{callsign}`"));

    private static SlashCommandResponse Ephemeral(string text) => new()
    {
        ResponseType = ResponseType.Ephemeral,
        Message = new Message { Text = text }
    };
}
