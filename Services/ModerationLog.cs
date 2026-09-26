namespace devanewbot.Services;

using System;
using System.Threading.Tasks;
using devanewbot.Data;
using devanewbot.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlackNet;
using SlackNet.WebApi;

public class ModerationLog(
    DevanewbotContext db,
    ISlackApiClient client,
    SlackDirectory directory,
    IOptions<ModerationOptions> moderationOptions,
    IOptions<SiteOptions> siteOptions,
    ILogger<ModerationLog> logger)
{
    protected ModerationOptions Options { get; } = moderationOptions.Value;
    protected SiteOptions Site { get; } = siteOptions.Value;

    public async Task RecordFromSlack(
        ModerationActionKind kind,
        string action,
        string reason,
        string administratorSlackUserId,
        string? targetSlackUserId = null,
        string? channelId = null,
        string? removedMessageText = null) =>
        await Record(new ModerationAction
        {
            Kind = kind,
            Source = ModerationActionSource.Slack,
            Action = action,
            Reason = reason,
            Administrator = await directory.Name(administratorSlackUserId),
            AdministratorSlackUserId = administratorSlackUserId,
            TargetSlackUserId = targetSlackUserId,
            ChannelId = channelId,
            RemovedMessageText = removedMessageText
        });

    public async Task Record(ModerationAction action)
    {
        if (action.OccurredAt == default)
        {
            action.OccurredAt = DateTime.UtcNow;
        }

        db.ModerationActions.Add(action);
        await db.SaveChangesAsync();

        if (!Options.AnnounceConfigured)
        {
            logger.LogWarning("Moderation:AnnounceChannelId is not set, so moderation action {ModerationActionId} was not announced", action.Id);
            return;
        }

        var text = $"{Escape(action.Administrator)}: {Escape(action.Action)}";
        if (!string.IsNullOrWhiteSpace(action.Reason))
        {
            text += $"\n>{Escape(action.Reason).ReplaceLineEndings("\n>")}";
        }

        if (Site.BaseUrlConfigured)
        {
            text += $"\n<{Site.BaseUrl!.TrimEnd('/')}/moderation|Moderation log>";
        }

        try
        {
            await client.Chat.PostMessage(new Message { Channel = Options.AnnounceChannelId, Text = text, UnfurlLinks = false });
        }
        catch (SlackException e)
        {
            logger.LogWarning(e, "Could not announce moderation action {ModerationActionId}", action.Id);
        }
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
