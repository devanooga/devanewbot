namespace devanewbot.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using devanewbot.Data.Models;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;

public class ModLogCommand(
    ISlackApiClient client,
    SlackDirectory directory,
    ModerationLog moderationLog) : ISlashCommandHandler, IViewSubmissionHandler
{
    public const string ModalCallbackId = "modlog-modal";
    private const string ActionInputId = "action";
    private const string ReasonInputId = "reason";

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        if (!await directory.IsAdmin(command.UserId))
        {
            return Reply("You do not have permission to use this command.");
        }

        await client.Views.Open(command.TriggerId, new ModalViewDefinition
        {
            Title = "Log moderation action",
            CallbackId = ModalCallbackId,
            Blocks =
            {
                new InputBlock
                {
                    BlockId = ActionInputId,
                    Label = "What was done",
                    Element = new PlainTextInput { ActionId = ActionInputId, InitialValue = command.Text?.Trim() }
                },
                new InputBlock
                {
                    BlockId = ReasonInputId,
                    Label = "Reason",
                    Element = new PlainTextInput { ActionId = ReasonInputId, Multiline = true }
                }
            },
            Submit = "Log it",
            Close = "Cancel"
        });

        return Reply("Complete the form to log a moderation action.");
    }

    public async Task<ViewSubmissionResponse> Handle(ViewSubmission viewSubmission)
    {
        var action = viewSubmission.View.State.GetValue<PlainTextInputValue>(ActionInputId).Value.Trim();
        var reason = viewSubmission.View.State.GetValue<PlainTextInputValue>(ReasonInputId).Value.Trim();

        if (!await directory.IsAdmin(viewSubmission.User.Id))
        {
            return new ViewErrorsResponse
            {
                Errors = new Dictionary<string, string> { [ActionInputId] = "Only workspace admins can log moderation actions." }
            };
        }

        await moderationLog.RecordFromSlack(ModerationActionKind.Other, action, reason, viewSubmission.User.Id);
        return ViewSubmissionResponse.Null;
    }

    public Task HandleClose(ViewClosed viewClosed) => Task.CompletedTask;

    private static SlashCommandResponse Reply(string text) =>
        new() { ResponseType = ResponseType.Ephemeral, Message = new Message { Text = text } };
}
