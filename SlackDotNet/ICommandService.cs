namespace SlackDotNet;

using System.Threading.Tasks;
using SlackDotNet.Webhooks;

public interface ICommandService
{
    Task HandleMessage(WebhookMessage message, string commandSuffix);

    Task HandleInteractive(InteractiveMessage message);
}
