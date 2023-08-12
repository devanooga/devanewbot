namespace devanewbot.Services;

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.Interaction;

public class SpongebobCommand : ISlashCommandHandler
{
    private Random Random = new Random();
    protected ISlackApiClient Client { get; }
    protected ILogger<SpongebobCommand> Logger { get; }

    public SpongebobCommand(ISlackApiClient client, ILogger<SpongebobCommand> logger)
    {
        Client = client;
        Logger = logger;
    }

    public string Response(string text)
    {
        // spONgEbOB cASe THE TexT
        text = new string(text.Select(c => Spongebobify(c)).ToArray());

        return $"{text} :spongebobmock:";
    }

    /// <summary>
    /// Takes char and randomly converts it to uppercase or lowercase
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private char Spongebobify(char c)
    {
        return Random.Next(2) == 0 ? Char.ToUpper(c) : Char.ToLower(c);
    }

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        var user = await Client.Users.Info(command.UserId);
        _ = Client.Chat.PostMessage(new SlackNet.WebApi.Message
        {
            Channel = command.ChannelId,
            Username = command.UserName,
            IconUrl = user.Profile.Image192,
            Text = Response(command.Text),
        });
        return null;
    }
}
