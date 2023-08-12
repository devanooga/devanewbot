namespace devanewbot.Services;

using System.Diagnostics;
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
        var perlStartInfo = new ProcessStartInfo(@"perl")
        {
            // MAKE SURE WE ESCAPE DOUBLE QUOTES DON'T RUIN MY DAY I SWEAR
            Arguments = $"{Directory}lib/{command.Command} \"{command.Text.Replace("\"", "\\\"")}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false
        };

        var perl = new Process
        {
            StartInfo = perlStartInfo
        };

        perl.Start();
        await perl.WaitForExitAsync();
        string output = await perl.StandardOutput.ReadToEndAsync() + await perl.StandardError.ReadToEndAsync();
        await Client.Chat.PostMessage(new Message
        {
            Username = "QRMBot",
            Channel = command.ChannelId,
            Text = output
        });
    }
}
