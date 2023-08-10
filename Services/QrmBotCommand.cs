namespace devanewbot.Services;

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlackDotNet;
using SlackDotNet.Payloads;
using SlackDotNet.Webhooks;

public class QrmBotCommand : Command
{

    protected string Directory { get; }

    public QrmBotCommand(string command, Slack slack, IConfiguration configuration, ILogger<QrmBotCommand> logger) : base(command, slack, configuration, logger)
    {
        Logger.LogInformation("Loadded QRM Bot Command: {}", command);
        Directory = configuration.GetSection("QRMBot").GetValue<string>("Directory");
    }

    protected override async Task HandleMessage(WebhookMessage webhookMessage)
    {
        var perlStartInfo = new ProcessStartInfo(@"perl")
        {
            Arguments = $"{Directory}qrmbot/lib/{CommandText} \"{webhookMessage.Text}\"",
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
        await Slack.PostMessage(new ChatMessage
        {
            Channel = webhookMessage.ChannelId,
            Text = output,
        });
    }
}
