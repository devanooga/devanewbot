namespace devanewbot.Services;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using devanewbot.Models;
using Microsoft.Extensions.Logging;
using SlackNet.Blocks;
using SlackNet.Interaction;

public class RemoveBanModalHandler(
    IChannelBanService channelBanService,
    ILogger<RemoveBanModalHandler> logger) : IViewSubmissionHandler
{
    public const string ModalCallbackId = "remove-channel-ban-modal";

    public async Task<ViewSubmissionResponse> Handle(ViewSubmission viewSubmission)
    {
        var metadata = JsonSerializer.Deserialize<ChannelBanMetadata>(viewSubmission.View.PrivateMetadata)
            ?? throw new InvalidOperationException("Metadata not found");

        var userToUnban = viewSubmission.View.State.GetValue<UserSelectValue>(RemoveBanCommand.UserInputId).SelectedUser;

        try
        {
            await channelBanService.RemoveBan(metadata.ChannelId, userToUnban);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to remove ban");
            return new ViewErrorsResponse
            {
                Errors = new Dictionary<string, string>
                {
                    { RemoveBanCommand.UserInputId, "Failed to remove ban" }
                }
            };
        }

        return ViewSubmissionResponse.Null;
    }

    public Task HandleClose(ViewClosed viewClosed)
    {
        throw new NotImplementedException();
    }
}