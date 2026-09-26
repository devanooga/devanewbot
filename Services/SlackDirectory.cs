namespace devanewbot.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SlackNet;
using SlackNet.WebApi;

public record SlackPerson(string Id, string Handle, string Name);

public record SlackChannel(string Id, string Name);

public class SlackDirectory(ISlackApiClient client, IMemoryCache cache)
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyList<SlackPerson>> People() =>
        (await cache.GetOrCreateAsync("slack-directory-people", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;
            var people = new List<SlackPerson>();
            string? cursor = null;
            do
            {
                var page = await client.Users.List(cursor, limit: 500);
                people.AddRange(page.Members
                    .Where(member => !member.Deleted && !member.IsBot && member.Id != "USLACKBOT")
                    .Select(member => new SlackPerson(member.Id, member.Name, DisplayName(member))));
                cursor = page.ResponseMetadata?.NextCursor;
            } while (!string.IsNullOrEmpty(cursor));

            return (IReadOnlyList<SlackPerson>)people.OrderBy(person => person.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }))!;

    public async Task<IReadOnlyList<SlackChannel>> Channels() =>
        (await cache.GetOrCreateAsync("slack-directory-channels", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;
            var channels = new List<SlackChannel>();
            string? cursor = null;
            do
            {
                var page = await client.Conversations.List(true, 1000, [ConversationType.PublicChannel], cursor);
                channels.AddRange(page.Channels.Select(channel => new SlackChannel(channel.Id, channel.Name)));
                cursor = page.ResponseMetadata?.NextCursor;
            } while (!string.IsNullOrEmpty(cursor));

            return (IReadOnlyList<SlackChannel>)channels.OrderBy(channel => channel.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }))!;

    public async Task<bool> IsAdmin(string userId)
    {
        var user = await client.Users.Info(userId);
        return !user.Deleted && (user.IsAdmin || user.IsOwner || user.IsPrimaryOwner);
    }

    public async Task<string> Name(string userId)
    {
        try
        {
            return DisplayName(await client.Users.Info(userId));
        }
        catch (SlackException)
        {
            return userId;
        }
    }

    public async Task<string> ChannelLabel(string channelId)
    {
        if (channelId.StartsWith('D'))
        {
            return "a direct message";
        }

        var channel = (await Channels()).FirstOrDefault(channel => channel.Id == channelId);
        return channel is null ? "a private channel" : $"#{channel.Name}";
    }

    private static string DisplayName(User member) =>
        new[] { member.Profile?.DisplayName, member.RealName, member.Name }
            .First(name => !string.IsNullOrWhiteSpace(name))!;
}
