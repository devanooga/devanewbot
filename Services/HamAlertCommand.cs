namespace devanewbot.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlackNet.Interaction;
using SlackNet.WebApi;

public partial class HamAlertCommand(HamAlertService hamAlert, ILogger<HamAlertCommand> logger) : ISlashCommandHandler
{
    private const string Usage = "Usage: `/hamalert list`, `/hamalert add W1AW [K4XYZ ...]`, `/hamalert remove W1AW`";

    [GeneratedRegex("^[A-Z0-9]{3,12}$")]
    private static partial Regex CallsignPattern { get; }

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        var arguments = (command.Text ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var action = arguments.FirstOrDefault()?.ToLowerInvariant() ?? "list";
        var callsigns = arguments.Skip(1).Select(callsign => callsign.ToUpperInvariant()).ToArray();

        if (action is not "list" && callsigns.Length == 0)
        {
            return Ephemeral(Usage);
        }

        var invalid = callsigns.Where(callsign => !CallsignPattern.IsMatch(callsign)).ToArray();
        if (invalid.Length > 0)
        {
            return Ephemeral($"Not a callsign: {Format(invalid)}");
        }

        try
        {
            return action switch
            {
                "list" => Ephemeral(await Describe()),
                "add" => Announce(command.UserId, "added", await hamAlert.AddCallsigns(callsigns), callsigns, "already on"),
                "remove" or "rm" or "delete" => Announce(command.UserId, "removed", await hamAlert.RemoveCallsigns(callsigns), callsigns, "not on"),
                _ => Ephemeral(Usage)
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "HamAlert command failed");
            return Ephemeral($":warning: {exception.Message}");
        }
    }

    private async Task<string> Describe()
    {
        var callsigns = await hamAlert.ListCallsigns();

        return callsigns.Length == 0
            ? $"The {hamAlert.ListName} alert list is empty."
            : $"{hamAlert.ListName} alert list ({callsigns.Length}): {Format(callsigns)}";
    }

    private SlashCommandResponse Announce(string userId, string verb, string[] changed, string[] requested, string skippedReason)
    {
        var skipped = requested.Except(changed).ToArray();
        var lines = new List<string>();

        if (changed.Length > 0)
        {
            lines.Add($"<@{userId}> {verb} {Format(changed)} {(verb == "added" ? "to" : "from")} the {hamAlert.ListName} alert list.");
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
