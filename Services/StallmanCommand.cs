namespace devanewbot.Services;

using System;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.Interaction;
using SlackNet.WebApi;

public class StallmanCommand : ISlashCommandHandler
{
    private Random Random = new Random();
    private const string Gallery = "https://stallman.org/photos/rms-working/";

    protected ISlackApiClient Client { get; }
    protected ILogger<StallmanCommand> Logger { get; }

    public StallmanCommand(ISlackApiClient client, ILogger<StallmanCommand> logger)
    {
        Client = client;
        Logger = logger;
    }

    public string ResponseAsync(string text)
    {
        // Download the gallery page and get random link
        var galleryPage = new HtmlWeb().Load(Gallery);
        var links = galleryPage.DocumentNode
                .SelectNodes("//p[3]/a");
        var link = Gallery + links[Random.Next(links.Count)]
                .Attributes["href"].Value;

        // Download the resulting page and get random image link
        var imagePage = new HtmlWeb().Load(link);

        return Gallery + imagePage.DocumentNode
                .SelectNodes("//p[2]/a/img")
                .First()
                .GetAttributeValue("src", "")
                .Substring(3);
    }

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        var slackUser = await Client.Users.Info(command.UserId);
        await Client.Chat.PostMessage(new Message
        {
            Channel = command.ChannelId,
            Username = slackUser.Profile.DisplayName,
            Text = ResponseAsync(command.Text),
            Parse = ParseMode.Full,
            UnfurlMedia = true,
            UnfurlLinks = true,
            IconUrl = slackUser.Profile.ImageOriginal
        });
        return null;
    }
}
