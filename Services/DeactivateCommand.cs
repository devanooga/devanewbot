namespace devanewbot.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using devanewbot.Data.Models;
using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;
using Slack = global::SlackDotNet.Slack;

public class DeactivateCommand(
    ISlackApiClient client,
    Slack slack,
    SlackDirectory directory,
    ModerationLog moderationLog,
    ILogger<DeactivateCommand> logger) : ISlashCommandHandler, IViewSubmissionHandler
{
    public const string ModalCallbackId = "deactivate-modal";
    private const string UserInputId = "user";
    private const string ReasonInputId = "reason";

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        if (!await directory.IsAdmin(command.UserId))
        {
            return Reply("You do not have permission to use this command.");
        }

        await client.Views.Open(command.TriggerId, new ModalViewDefinition
        {
            Title = "Deactivate account",
            CallbackId = ModalCallbackId,
            Blocks =
            {
                new InputBlock
                {
                    BlockId = UserInputId,
                    Label = "Person",
                    Element = new UserSelectMenu { ActionId = UserInputId }
                },
                new InputBlock
                {
                    BlockId = ReasonInputId,
                    Label = "Reason",
                    Hint = "Shown on the public moderation log.",
                    Element = new PlainTextInput { ActionId = ReasonInputId, Multiline = true }
                }
            },
            Submit = "Deactivate",
            Close = "Cancel"
        });

        return Reply("Complete the form to deactivate an account.");
    }

    public async Task<ViewSubmissionResponse> Handle(ViewSubmission viewSubmission)
    {
        var administratorId = viewSubmission.User.Id;
        var userId = viewSubmission.View.State.GetValue<UserSelectValue>(UserInputId).SelectedUser;
        var reason = viewSubmission.View.State.GetValue<PlainTextInputValue>(ReasonInputId).Value.Trim();

        if (!await directory.IsAdmin(administratorId))
        {
            return Error(ReasonInputId, "Only workspace admins can deactivate accounts.");
        }

        if (await directory.IsAdmin(userId))
        {
            return Error(UserInputId, "Admins cannot be deactivated from here.");
        }

        var name = await directory.Name(userId);
        var (success, error) = await slack.DisableUser(userId);
        if (!success)
        {
            logger.LogWarning("Could not deactivate {UserId}: {Error}", userId, error);
            return Error(UserInputId, $"Slack refused to deactivate them ({error}).");
        }

        await moderationLog.RecordFromSlack(ModerationActionKind.Deactivated, $"Deactivated \"{name}\"", reason, administratorId, userId);
        return ViewSubmissionResponse.Null;
    }

    public Task HandleClose(ViewClosed viewClosed) => Task.CompletedTask;

    private static SlashCommandResponse Reply(string text) =>
        new() { ResponseType = ResponseType.Ephemeral, Message = new Message { Text = text } };

    private static ViewErrorsResponse Error(string blockId, string message) =>
        new() { Errors = new Dictionary<string, string> { [blockId] = message } };
}
