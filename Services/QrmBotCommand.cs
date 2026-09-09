namespace devanewbot.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.Interaction;
using SlackNet.WebApi;

public class QrmBotCommand : ISlashCommandHandler
{
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["wx"] = "aeris"
    };

    protected ILogger<QrmBotCommand> Logger { get; }

    protected string ToolDirectory { get; }

    protected ISlackApiClient Client { get; }

    public QrmBotCommand(ISlackApiClient client, IConfiguration configuration, ILogger<QrmBotCommand> logger)
    {
        Logger = logger;
        Client = client;
        ToolDirectory = Path.Combine(configuration.GetSection("QRMBot").GetValue<string>("Directory") ?? string.Empty, "lib");
    }

    public Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        var arguments = ParseArguments(command.Text ?? string.Empty);
        var requested = arguments.FirstOrDefault()?.ToLowerInvariant();
        var tool = requested is not null && Aliases.TryGetValue(requested, out var alias) ? alias : requested;
        var tools = AvailableTools();

        if (tool is null or "help")
        {
            return Task.FromResult(Ephemeral($"Usage: `/qrm <tool> [arguments]`\n{ToolList(tools)}"));
        }

        if (!tools.Contains(tool))
        {
            return Task.FromResult(Ephemeral($"`{tool}` is not a QRMBot tool.\n{ToolList(tools)}"));
        }

        _ = RunCommand(tool, arguments.Skip(1), command.ChannelId);
        return Task.FromResult<SlashCommandResponse>(null);
    }

    protected IReadOnlyCollection<string> AvailableTools()
    {
        if (!Directory.Exists(ToolDirectory))
        {
            Logger.LogError("QRMBot directory {ToolDirectory} does not exist", ToolDirectory);
            return [];
        }

        return
        [
            .. Directory.EnumerateFiles(ToolDirectory)
                .Select(Path.GetFileName)
                .Where(tool => !tool!.EndsWith(".csv") && !tool.EndsWith("pm"))
                .Order(StringComparer.Ordinal)!
        ];
    }

    protected async Task RunCommand(string tool, IEnumerable<string> arguments, string channelId)
    {
        var perlStartInfo = new ProcessStartInfo(@"perl")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false
        };

        perlStartInfo.ArgumentList.Add(Path.Combine(ToolDirectory, tool));
        foreach (var argument in arguments)
        {
            perlStartInfo.ArgumentList.Add(argument);
        }

        var perl = new Process
        {
            StartInfo = perlStartInfo
        };

        perl.Start();
        var stdoutTask = perl.StandardOutput.ReadToEndAsync();
        var stderrTask = perl.StandardError.ReadToEndAsync();
        await perl.WaitForExitAsync();
        string output = await stdoutTask + await stderrTask;
        await Client.Chat.PostMessage(new Message
        {
            Username = "QRMBot",
            Channel = channelId,
            Text = output
        });
    }

    // We want to support quoted arguments with spaces in them, but also just normal space-separated arguments
    private static string[] ParseArguments(string text) =>
        [.. Regex.Matches(text, @"[""].+?[""]|[^ ]+").Select(match => match.Value.Trim('"'))];

    private static string ToolList(IReadOnlyCollection<string> tools) => $"```\n{string.Join(" ", tools)}\n```";

    private static SlashCommandResponse Ephemeral(string text) => new()
    {
        ResponseType = ResponseType.Ephemeral,
        Message = new Message { Text = text }
    };
}
