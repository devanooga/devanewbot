namespace devanewbot.Services;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using devanewbot.Models;
using Microsoft.Extensions.Logging;
using SlackNet.Blocks;
using SlackNet.Interaction;

public class ChannelBanModalHandler(
    IChannelBanService channelBanService,
    ILogger<ChannelBanModalHandler> logger) : IViewSubmissionHandler
{
    public const string ModalCallbackId = "channel-ban-modal";

    public async Task<ViewSubmissionResponse> Handle(ViewSubmission viewSubmission)
    {
        var banningUser = viewSubmission.User.Id;
        var metadata = JsonSerializer.Deserialize<ChannelBanMetadata>(viewSubmission.View.PrivateMetadata)
            ?? throw new InvalidOperationException("Metadata not found");

        var userToBan = viewSubmission.View.State.GetValue<UserSelectValue>(ChannelBanCommand.UserInputId).SelectedUser;
        var reason = viewSubmission.View.State.GetValue<PlainTextInputValue>(ChannelBanCommand.ReasonInputId).Value;
        var endDate = viewSubmission.View.State.GetValue<DatePickerValue>(ChannelBanCommand.EndDateInputId).SelectedDate;

        try
        {
            await channelBanService.AddBan(metadata.ChannelId, userToBan, banningUser, reason, endDate);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to add ban");
            return new ViewErrorsResponse
            {
                Errors = new Dictionary<string, string>
                {
                    { ChannelBanCommand.UserInputId, "Failed to add ban" }
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