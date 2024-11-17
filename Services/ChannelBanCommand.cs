namespace devanewbot.Services;

using System.Text.Json;
using System.Threading.Tasks;
using devanewbot.Models;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;

public class ChannelBanCommand(ISlackApiClient client) : ISlashCommandHandler
{
    public const string ActionId = "channel-ban";
    public const string UserInputId = "banned-user";
    public const string ReasonInputId = "reason";
    public const string EndDateInputId = "end-date";

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        var userId = command.UserId;
        var user = await client.Users.Info(userId);

        if (user.IsAdmin is false)
        {
            return new SlashCommandResponse
            {
                ResponseType = ResponseType.Ephemeral,
                Message = new Message
                {
                    Text = "You do not have permission to use this command."
                }
            };
        }

        await client.Views.Open(command.TriggerId, new ModalViewDefinition
        {
            Title = "Create Channel Ban",
            CallbackId = ChannelBanModalHandler.ModalCallbackId,
            Blocks =
                    {
                        new InputBlock
                        {
                            Label = "User to Ban",
                            Optional = false,
                            Element = new UserSelectMenu
                                {
                                    ActionId = UserInputId,
                                }
                        },
                        new InputBlock
                        {
                            Label = "Reason",
                            Optional = false,
                            Element = new PlainTextInput
                            {
                                ActionId = ReasonInputId,
                                Multiline = true,
                            }
                        },
                        new InputBlock
                        {
                            Label = "End Date",
                            Optional = true,
                            Element = new DatePicker
                            {
                                ActionId = EndDateInputId,
                            }
                        }
                    },
            Submit = ":banhammer: Ban",
            Close = "Cancel",
            NotifyOnClose = false,
            PrivateMetadata = JsonSerializer.Serialize(new ChannelBanMetadata(command.ChannelId))
        });

        return new SlashCommandResponse
        {
            ResponseType = ResponseType.Ephemeral,
            Message = new Message
            {
                Text = "Complete the form to issue a channel ban."
            }
        };
    }
}