namespace devanewbot.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlackDotNet;
using SlackDotNet.Webhooks;

public class CommandService : ICommandService
{
    private List<Command> Commands { get; set; }
    private ILogger<CommandService> Logger { get; set; }

    public CommandService(GifCommand gifCommand, SpongebobCommand spongebobCommand, StallmanCommand stallmanCommand, ILogger<CommandService> logger)
    {
        Commands = new List<Command>()
        {
            gifCommand,
            spongebobCommand,
            stallmanCommand
        };
        Logger = logger;
    }

    public async Task HandleMessage(WebhookMessage message, string commandSuffix = "")
    {
        var messageCommand = message.Command.Substring(1); // Remove leading forward-slash
        messageCommand = messageCommand.Substring(0, messageCommand.Length - commandSuffix.Length); // Remove any optional suffix

        var command = Commands.Find((c) => c.CommandText == messageCommand);

        await command.ExecuteAsync(message);
    }

    public async Task HandleInteractive(InteractiveMessage message)
    {
        var command = Commands.Find((c) => c.CommandText == message.CallbackId);

        await command.HandleInteractive(message);
    }
}
