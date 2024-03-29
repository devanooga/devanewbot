namespace devanewbot.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlackDotNet;
using SlackDotNet.Webhooks;

public class CommandService : ICommandService
{
    private List<Command> Commands { get; set; }
    private ILogger<CommandService> Logger { get; set; }
    protected IServiceProvider ServiceProvider { get; set; }
    protected IServiceScope ServiceScope { get; set; }

    public CommandService(
        IServiceProvider serviceProvider,
        GifCommand gifCommand, SpongebobCommand spongebobCommand, StallmanCommand stallmanCommand, ILogger<CommandService> logger)
    {
        // Register new commands here by adding it to the constructor and appending it to the list.
        Commands = new List<Command>()
        {
            gifCommand,
            spongebobCommand,
            stallmanCommand
        };

        // "No!" says the man in Github, "this should be a factory"
        //  I choose the lazy solution, I choose... this.
        Commands.AddRange(Directory.EnumerateFiles("qrmbot/lib")
            .Select(f => new QrmBotCommand(
                f.Split("/").Last().Replace(".pl", string.Empty),
                serviceProvider.GetRequiredService<Slack>(),
                serviceProvider.GetRequiredService<IConfiguration>(),
                serviceProvider.GetRequiredService<ILogger<QrmBotCommand>>()))
            .ToArray());
        Logger = logger;
    }

    /// <summary>
    /// Finds the command with the `CommandText` that matches the command from
    /// the WebhookMessage and calls `Execute` on that command.
    ///
    /// If an optional "commandSuffix" is provided (via configuration), that is
    /// stripped from the message's command before searching the list of commands.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="commandSuffix"></param>
    /// <returns></returns>
    public async Task HandleMessage(WebhookMessage message, string commandSuffix = "")
    {
        try
        {
            // Remove leading forward-slash and any optional suffix
            var messageCommand = message.Command.Substring(1);
            messageCommand = messageCommand.Substring(0, messageCommand.Length - commandSuffix.Length);

            var command = Commands.Find((c) => c.CommandText == messageCommand);

            await command.Execute(message);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Error running command: {}", message.Command);
        }
    }

    /// <summary>
    /// Finds the command with the `CommandText` that matches the command from
    /// the InteractiveMessage and calls `Execute` on that command.
    ///
    /// Unlike `HandleMessage`, no suffix is required.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task HandleInteractive(InteractiveMessage message)
    {
        var command = Commands.Find((c) => c.CommandText == message.CallbackId);

        try
        {
            Logger.LogError("Test: {}", message.CallbackId);
            await command.ExecuteInteractive(message);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Error running command: {}", message.CallbackId);
        }
    }
}
