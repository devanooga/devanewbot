namespace SlackDotNet;

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlackDotNet.Webhooks;

/// <summary>
/// Abstract class for all /slashCommands
/// </summary>
public abstract class Command
{
    public readonly string CommandText;
    public readonly Slack Slack;
    public readonly IConfiguration Configuration;
    public readonly ILogger<Command> Logger;

    public Command(string commandText, Slack slack, IConfiguration configuration, ILogger<Command> logger)
    {
        CommandText = commandText;
        Slack = slack;
        Configuration = configuration;
        Logger = logger;
    }

    /// <summary>
    /// This is called after the webhook is validated.
    ///
    /// Any task that the command needs to do should be done here.
    /// </summary>
    /// <param name="webhookMessage"></param>
    /// <returns></returns>
    protected abstract Task HandleMessage(WebhookMessage webhookMessage);

    /// <summary>
    /// Interactive commands (i.e. GifCommand) can receive additional messages to allow interactivity.
    ///
    /// Any task that needs to handle interactive message should override this.
    /// </summary>
    /// <param name="interactiveMessage"></param>
    /// <returns></returns>
    protected virtual async Task HandleInteractive(InteractiveMessage interactiveMessage)
    {
        await Task.Run(() => Logger.LogWarning("HandleInteractive called on a non-interactive command"));
    }

    /// <summary>
    /// Public interface for running command's interactive task.
    /// </summary>
    /// <param name="interactiveMessage"></param>
    /// <returns></returns>
    public async Task ExecuteInteractive(InteractiveMessage interactiveMessage)
    {
        await HandleInteractive(interactiveMessage);
    }

    /// <summary>
    /// Public interface for running command's task.
    /// Before switching to WebSockets, we needed to validate each message. That step
    /// is no longer required.
    /// </summary>
    /// <param name="webhookMessage"></param>
    /// <returns></returns>
    public async Task Execute(WebhookMessage webhookMessage)
    {
        await HandleMessage(webhookMessage);
    }
}
