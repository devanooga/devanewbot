namespace devanewbot.Services;

using System;
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
    protected ILogger<QrmBotCommand> Logger { get; }

    protected string Directory { get; }

    protected ISlackApiClient Client { get; }

    public QrmBotCommand(ISlackApiClient client, IConfiguration configuration, ILogger<QrmBotCommand> logger)
    {
        Logger = logger;
        Client = client;
        Directory = configuration.GetSection("QRMBot").GetValue<string>("Directory");
    }

    public Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        _ = RunCommand(command);
        return Task.FromResult<SlashCommandResponse>(null);
    }

    protected async Task RunCommand(SlashCommand command)
    {
        // Remove first slash
        var cmd = command.Command.Substring(1).Replace("/wx", "/aeris");
        // Directory transversal should not be possible because the command must be
        //  provided as an approved list from Slack, but just in case, let's be safe here
        if (cmd.Contains(".."))
        {
            throw new ArgumentException("Invalid command");
        }

        var perlStartInfo = new ProcessStartInfo(@"perl")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false
        };

        // We to support quoted arguments with spaces in them, but also just normal space-separated arguments
        var args = Regex.Matches(command.Text, @"[""].+?[""]|[^ ]+")
            .Select(m => m.Value.Trim('"'));
        perlStartInfo.ArgumentList.Add($"{Directory}lib/{cmd}");
        foreach (var arg in args)
        {
            perlStartInfo.ArgumentList.Add(arg);
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
            Channel = command.ChannelId,
            Text = output
        });
    }
}
