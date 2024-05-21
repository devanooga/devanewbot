namespace devanewbot.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlackDotNet;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;
using WebSocket4Net.Command;

public class InviteService : IBlockActionHandler<ButtonAction>
{
    protected Slack Slack { get; }

    protected ISlackApiClient SlackApiClient { get; }
    protected ILogger<InviteService> Logger
    { get; }

    private string Channel = "C074VF1PC7K";

    public InviteService(Slack slack,
         ISlackApiClient slackApiClient,
         ILogger<InviteService> logger)
    {
        Slack = slack;
        SlackApiClient = slackApiClient;
        Logger = logger;
    }

    public async Task CreateInvite(string email, string ip)
    {
        var payload = JsonSerializer.Serialize(new SignupPayload
        {
            Email = email,
            Ip = ip
        });

        await SlackApiClient.Chat.PostMessage(new Message
        {
            Channel = Channel,
            Blocks = new Block[] {
                new SectionBlock
                {
                    Text = new Markdown($"Sending invite to {email} from IP {ip}")
                },
                new ActionsBlock
                {
                    Elements =
                    {
                        new SlackNet.Blocks.Button
                        {

                            ActionId = "approve_invite",
                            Text = "Approve Invite",
                            Value = payload,
                            Style = ButtonStyle.Default
                        },
                        new SlackNet.Blocks.Button
                        {

                            ActionId = "decline_invite",
                            Text = "Decline Invite",
                            Value = payload,
                            Style = ButtonStyle.Danger
                        },
                    }
                }
            }
        });
    }

    public async Task Handle(ButtonAction action, BlockActionRequest request)
    {
        Logger.LogInformation("Action: {}", action.ActionId);
        var commandingUser = await SlackApiClient.Users.Info(request.User.Id);
        var signup = JsonSerializer.Deserialize<SignupPayload>(action.Value);
        switch (action.ActionId)
        {
            case "decline_invite":
                await SlackApiClient.Respond(request.ResponseUrl, new SlackNet.Interaction.MessageUpdateResponse(new MessageResponse { DeleteOriginal = true }), null);
                await SlackApiClient.Chat.PostMessage(new Message
                {
                    Channel = request.Channel.Id,
                    Parse = ParseMode.Full,
                    Text = $"{commandingUser.Profile.DisplayName} denied invite for {signup.Email} from {signup.Ip}",
                    UnfurlLinks = true,
                });
                break;
            case "approve_invite":
                var (success, error) = await Slack.InviteUser(signup.Email);
                await SlackApiClient.Respond(request.ResponseUrl, new SlackNet.Interaction.MessageUpdateResponse(new MessageResponse { DeleteOriginal = true }), null);
                if (!success)
                {
                    await SlackApiClient.Chat.PostMessage(new Message
                    {
                        Channel = request.Channel.Id,
                        Parse = ParseMode.Full,
                        Text = $"{commandingUser.Profile.DisplayName} approved {signup.Email} from {signup.Ip} but we had an error: {error}",
                        UnfurlLinks = true,
                    });
                }
                else
                {
                    await SlackApiClient.Chat.PostMessage(new Message
                    {
                        Channel = request.Channel.Id,
                        Parse = ParseMode.Full,
                        Text = $"{commandingUser.Profile.DisplayName} approved {signup.Email} from {signup.Ip}",
                        UnfurlLinks = true,
                    });
                }

                break;
        }
    }

    public class SignupPayload
    {
        public string Email { get; set; }
        public string Ip { get; set; }
    }

}