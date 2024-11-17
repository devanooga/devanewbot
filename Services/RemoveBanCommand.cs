namespace devanewbot.Services;

using System.Text.Json;
using System.Threading.Tasks;
using devanewbot.Models;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;

public class RemoveBanCommand(ISlackApiClient client) : ISlashCommandHandler
{
    public const string ActionId = "remove-ban";
    public const string UserInputId = "user-to-unban";

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
            CallbackId = RemoveBanModalHandler.ModalCallbackId,
            Blocks =
                    {
                        new InputBlock
                        {
                            Label = "User to Unban",
                            Optional = false,
                            Element = new UserSelectMenu
                                {
                                    ActionId = UserInputId,
                                }
                        },
                    },
            Submit = ":yay_fox: Unban",
            Close = "Cancel",
            NotifyOnClose = false,
            PrivateMetadata = JsonSerializer.Serialize(new ChannelBanMetadata(command.ChannelId))
        });

        return new SlashCommandResponse
        {
            ResponseType = ResponseType.Ephemeral,
            Message = new Message
            {
                Text = "Complete the form to remove a channel ban."
            }
        };
 
    }
}