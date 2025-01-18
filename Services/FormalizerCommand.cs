namespace devanewbot.Services;

using System;
using System.Threading.Tasks;
using Flurl.Http;
using SlackNet;
using SlackNet.Interaction;
using SlackNet.WebApi;

public class FormalizerCommand(ISlackApiClient client) : ISlashCommandHandler
{
    private readonly Uri Formalizer = new("https://goblin.tools/api/Formalizer");
    private readonly string[] Suffixes = [
        "probably",
        "1936 - colorized",
        "allegedly",
        "or something, idk I wasn't listening"
    ];
    private readonly Random Random = new();

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        // A link to a message is required and do some quick verification to make sure it's a valid message link.
        if (string.IsNullOrEmpty(command.Text)
            || !Uri.TryCreate(command.Text, UriKind.Absolute, out Uri? messageUri)
            || (messageUri.Host != "devanooga.slack.com" && messageUri.Segments[0] != "archives"))
        {
            return new SlashCommandResponse
            {
                ResponseType = ResponseType.Ephemeral,
                Message = new Message
                {
                    Text = "You must provide a link to a message to formalize."
                }
            };
        }

        var channelId = messageUri.Segments[2].Trim('/');
        var messageTs = messageUri.Segments[3].Trim(['/', 'p']).Insert(10, ".");

        var messageFetchResult = await client.Conversations.History(
            channelId,
            oldestTs: messageTs,
            inclusive: true,
            limit: 1
        );

        if (messageFetchResult.Messages.Count == 0)
        {
            return new SlashCommandResponse
            {
                ResponseType = ResponseType.Ephemeral,
                Message = new Message
                {
                    Text = "The message you provided does not exist."
                }
            };
        }

        var messageToFormalize = messageFetchResult.Messages[0];
        var messageText = messageToFormalize.Text;
        var messageUser = messageToFormalize.User;

        var formalizedMessage = await Formalizer.PostJsonAsync(new
        {
            Conversion = "professional",
            Spiciness = "3",
            Text = messageText
        }).ReceiveString();

        var slackUser = await client.Users.Info(command.UserId);

        await client.Chat.PostMessage(new Message
        {
            Channel = command.ChannelId,
            Username = slackUser.Profile.DisplayName,
            Text = $"> {formalizedMessage}\n\n-<@{messageUser}>, {Suffixes[Random.Next(Suffixes.Length)]}",
            Parse = ParseMode.Client,
            UnfurlMedia = true,
            UnfurlLinks = false,
            IconUrl = slackUser.Profile.ImageOriginal
        });

        return null!;
    }
}
