namespace devanewbot.Services;

using System;
using System.Threading.Tasks;
using devanewbot.Models;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;

public class GifCommand : ISlashCommandHandler, IBlockActionHandler<SlackNet.Blocks.ButtonAction>
{
    private Random Random = new Random();
    protected IConfiguration Configuration { get; }
    protected ISlackApiClient Client { get; }
    protected ILogger<GifCommand> Logger { get; }

    public GifCommand(ISlackApiClient client, IConfiguration configuration, ILogger<GifCommand> logger)
    {
        Client = client;
        Configuration = configuration;
        Logger = logger;
    }

    private string SearchUri { get; set; } = "https://api.bing.microsoft.com/v7.0/images/search";

    public async Task<string> Response(SlashCommand command)
    {
        return await ImageSearch(command.Text);
    }

    private async Task<string> ImageSearch(string searchTerm, bool random = false)
    {
        var uri = SearchUri.SetQueryParam("q", searchTerm)
            .SetQueryParam("imageType", "AnimatedGifHttps")
            .SetQueryParam("safeSearch", "Strict")
            .WithHeader("Ocp-Apim-Subscription-Key", Configuration["BingSearch:Key"]);

        var response = await uri.GetJsonAsync<ImageSearch>();

        var count = response.Value.Count;
        if (random)
        {
            // Get a random gif
            return response.Value[Random.Next(count)].ContentUrl.ToString();
        }

        // Grab the first gif
        return response.Value[0].ContentUrl.ToString();
    }

    /// <summary>
    /// Handles the work of actually sending the Gif ephemeral dialog
    /// </summary>
    /// <param name="channelId"></param>
    /// <param name="displayName"></param>
    /// <param name="iconUrl"></param>
    /// <param name="userId"></param>
    /// <param name="text"></param>
    /// <param name="gifUrl"></param>
    /// <returns></returns>
    private async Task SendGif(string channelId, string displayName, Uri iconUrl, string userId, string text, string gifUrl)
    {
        await Client.Chat.PostEphemeral(userId, new Message
        {
            Channel = channelId,
            Username = displayName,
            Text = "Can do! :cando:",
            IconUrl = iconUrl.ToString(),
            Blocks = new Block[] {
                new ImageBlock
                {
                    ImageUrl = gifUrl,
                    AltText = text
                },
                new ActionsBlock
                {
                    Elements =
                    {
                        new SlackNet.Blocks.Button
                        {

                            ActionId = "post",
                            Text = "Post",
                            Value = gifUrl,
                            Style = ButtonStyle.Primary
                        },
                        new SlackNet.Blocks.Button
                        {
                            ActionId = "random",
                            Text = "Hit me baby one more time!",
                            Value = text,
                        },
                        new SlackNet.Blocks.Button
                        {
                            ActionId = "cancel",
                            Text = "Nevermind",
                            Value = "cancel",
                            Style = ButtonStyle.Danger
                        }
                    }
                }
            }
        });
    }

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        var slackUser = await Client.Users.Info(command.UserId);
        var url = await Response(command);
        await SendGif(
            command.ChannelId,
            slackUser.Profile.DisplayName,
            new Uri(slackUser.Profile.ImageOriginal),
            command.UserId,
            command.Text,
            url);
        return null;
    }

    public async Task Handle(ButtonAction action, BlockActionRequest request)
    {
        Logger.LogInformation("Action: {}", action.ActionId);
        var slackUser = await Client.Users.Info(request.User.Id);
        if (action.ActionId == "post")
        {
            await Client.Chat.PostMessage(new Message
            {
                Channel = request.Channel.Id,
                Username = slackUser.Profile.DisplayName,
                Parse = ParseMode.Full,
                Text = action.Value,
                UnfurlLinks = true,
                IconUrl = slackUser.Profile.ImageOriginal,
            });
        }
        else if (action.ActionId == "random")
        {
            var url = await ImageSearch(action.Value, true);
            await SendGif(request.Channel.Id,
                slackUser.Profile.DisplayName,
                new Uri(slackUser.Profile.ImageOriginal),
                slackUser.Id,
                action.Value,
                url);
        }


        await Client.Respond(request.ResponseUrl, new SlackNet.Interaction.MessageUpdateResponse(new MessageResponse { DeleteOriginal = true }), System.Threading.CancellationToken.None);
    }
}
