namespace devanewbot.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using devanewbot.Data.Models;
using devanewbot.SlackDotNet.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlackNet;
using SlackNet.WebApi;
using Slack = global::SlackDotNet.Slack;

public record AuthoredMessage(string ChannelId, string Channel, string Ts, DateTime PostedAt, string Text, string? Permalink);

public record MessageToRemove(string ChannelId, string Ts, string Text);

public record RemovalResult(int Removed, bool Deactivated, IReadOnlyList<string> Failures);

public class MessageRemoval(
    ISlackApiClient client,
    Slack slack,
    SlackDirectory directory,
    ModerationLog moderationLog,
    IOptions<SlackOptions> slackOptions,
    ILogger<MessageRemoval> logger)
{
    private const int SearchPageSize = 100;
    private const int MaxSearchPages = 5;

    // Bot tokens can neither search nor delete other people's messages; the admin user token can do both.
    private ISlackApiClient AsAdmin => client.WithAccessToken(slackOptions.Value.UserToken);

    public async Task<IReadOnlyList<AuthoredMessage>> Recent(string userId, TimeSpan window)
    {
        var since = DateTime.UtcNow - window;
        // Slack's after: is exclusive and day-granular, so ask for a day extra and trim by timestamp.
        var query = $"from:<@{userId}> after:{since.AddDays(-1):yyyy-MM-dd}";
        var messages = new List<AuthoredMessage>();

        for (var page = 1; page <= MaxSearchPages; page++)
        {
            var results = (await AsAdmin.Search.Messages(query, SortBy.Timestamp, SortDirection.Descending, count: SearchPageSize, page: page)).Messages;
            foreach (var match in results.Matches.Where(match => !match.Channel.IsIm && !match.Channel.IsMpim))
            {
                var postedAt = PostedAt(match.Ts);
                if (postedAt < since)
                {
                    return messages;
                }

                messages.Add(new AuthoredMessage(
                    match.Channel.Id,
                    string.IsNullOrEmpty(match.Channel.Name) ? await directory.ChannelLabel(match.Channel.Id) : $"#{match.Channel.Name}",
                    match.Ts,
                    postedAt,
                    match.Text ?? "",
                    match.Permalink));
            }

            if (page >= (results.Paging?.Pages ?? 1))
            {
                break;
            }
        }

        return messages;
    }

    public async Task<RemovalResult> Remove(
        string administratorId,
        string authorId,
        IReadOnlyList<MessageToRemove> messages,
        string reason,
        bool deactivate)
    {
        var removed = new List<MessageToRemove>();
        var failures = new List<string>();

        foreach (var message in messages)
        {
            var error = await Delete(message);
            if (error is null)
            {
                removed.Add(message);
            }
            else
            {
                failures.Add($"{await directory.ChannelLabel(message.ChannelId)} {message.Ts}: {error}");
            }
        }

        var name = await directory.Name(authorId);
        var deactivated = false;
        if (deactivate)
        {
            var (success, error) = await slack.DisableUser(authorId);
            deactivated = success;
            if (!success)
            {
                failures.Add($"Deactivating {name}: {error}");
            }
        }

        if (removed.Count > 0 || deactivated)
        {
            var channels = new List<string>();
            foreach (var channelId in removed.Select(message => message.ChannelId).Distinct())
            {
                channels.Add(await directory.ChannelLabel(channelId));
            }

            var action = removed.Count == 0
                ? $"Deactivated \"{name}\""
                : $"Removed {Plural(removed.Count, "post")} by {name} in {string.Join(", ", channels)}"
                    + (deactivated ? " and deactivated their account" : "");

            await moderationLog.RecordFromSlack(
                deactivated ? ModerationActionKind.Deactivated : ModerationActionKind.RemovedMessage,
                action,
                reason,
                administratorId,
                authorId,
                channels.Count == 1 ? removed[0].ChannelId : null,
                removed.Count == 0 ? null : await Transcript(removed));
        }

        return new RemovalResult(removed.Count, deactivated, failures);
    }

    private async Task<string?> Delete(MessageToRemove message)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await AsAdmin.Chat.Delete(message.Ts, message.ChannelId);
                return null;
            }
            catch (SlackRateLimitException e)
            {
                await Task.Delay(e.RetryAfter ?? TimeSpan.FromSeconds(1));
            }
            catch (SlackException e)
            {
                logger.LogWarning(e, "Could not delete message {Ts} in {ChannelId}", message.Ts, message.ChannelId);
                return e.ErrorCode;
            }
        }

        return "rate limited";
    }

    private async Task<string> Transcript(IEnumerable<MessageToRemove> messages)
    {
        var entries = new List<string>();
        foreach (var message in messages.OrderBy(message => message.Ts))
        {
            entries.Add($"{await directory.ChannelLabel(message.ChannelId)} {PostedAt(message.Ts):yyyy-MM-dd HH:mm} UTC\n{message.Text}");
        }

        return string.Join("\n\n", entries);
    }

    private static DateTime PostedAt(string ts) =>
        DateTime.UnixEpoch.AddSeconds(double.Parse(ts, CultureInfo.InvariantCulture));

    private static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
