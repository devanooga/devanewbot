namespace devanewbot.Services;

using System;
using System.Linq;
using System.Threading.Tasks;
using devanewbot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlackDotNet.Options;
using SlackNet;
using SlackNet.WebApi;

public class ChannelBanService(
    ILogger<ChannelBanService> logger,
    DevanewbotContext devanewbotContext,
    ISlackApiClient client,
    IOptions<SlackOptions> slackOptions) : IChannelBanService
{
    public async Task AddBan(string channelId, string userId, string banningUserId, string reason, DateTime? expiresAt = null)
    {
        logger.LogInformation(
            "Adding ban for user {UserId} in channel {ChannelId} by user {BanningUserId} with reason {Reason} and expiration {ExpiresAt}",
            userId,
            channelId,
            banningUserId,
            reason,
            expiresAt);

        var existingBan = await devanewbotContext.ChannelBans
            .FirstOrDefaultAsync(b => b.ChannelId == channelId && b.UserId == userId && b.Active);

        if (existingBan is not null)
        {
            logger.LogInformation("User {UserId} is already banned in channel {ChannelId}", userId, channelId);
            return;
        }

        devanewbotContext.ChannelBans.Add(new ChannelBan
        {
            UserId = userId,
            ChannelId = channelId,
            BannedBy = banningUserId,
            Reason = reason,
            BannedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt.HasValue ? expiresAt.Value.ToUniversalTime() : null,
            Active = true
        });
        await devanewbotContext.SaveChangesAsync();

        await client.WithAccessToken(slackOptions.Value.UserToken).Conversations.Kick(channelId, userId);

        // Alert the channel to the ban
        await client.Chat.PostMessage(new Message
        {
            Channel = channelId,
            Text = $"<@{userId}> has been banned from this channel by <@{banningUserId}> for \"{reason}\"."
                + (expiresAt.HasValue ? $" The ban will expire on {expiresAt.Value:yyyy-MM-dd}." : "")
        });

        // Alert the user to the ban
        await client.Chat.PostMessage(new Message
        {
            Channel = userId,
            Text = $"You have been banned in <#{channelId}> for \"{reason}\". Attempting to evade this ban will result in further action."
                + (expiresAt.HasValue ? $" The ban will expire on {expiresAt.Value:yyyy-MM-dd}." : "")
        });

    }

    public async Task RemoveBan(string channelId, string userId)
    {
        logger.LogInformation("Removing ban for user {UserId} in channel {ChannelId}", userId, channelId);

        var ban = await devanewbotContext.ChannelBans
            .FirstOrDefaultAsync(b => b.ChannelId == channelId && b.UserId == userId && b.Active);

        if (ban is null)
        {
            logger.LogInformation("User {UserId} is not banned in channel {ChannelId}", userId, channelId);
            return;
        }

        ban.Active = false;
        ban.LiftedEarly = DateTime.UtcNow < ban.ExpiresAt;
        await devanewbotContext.SaveChangesAsync();

        await client.Chat.PostMessage(new Message
        {
            Channel = userId,
            Text = $"You have been unbanned from <#{channelId}>."
        });

    }

    public async Task CheckExpirations()
    {
        logger.LogInformation("Checking for expired bans");

        var expiredBans = await devanewbotContext.ChannelBans
            .Where(b => b.ExpiresAt.HasValue && b.ExpiresAt.Value < DateTime.UtcNow && b.Active)
            .ToListAsync();

        foreach (var ban in expiredBans)
        {
            await RemoveBan(ban.ChannelId, ban.UserId);
        }
    }

    public async Task<bool> HasActiveBan(string channelId, string userId)
    {
        logger.LogInformation("Checking if user {UserId} is currently banned in channel {ChannelId}", userId, channelId);

        return await devanewbotContext.ChannelBans
            .AnyAsync(b => b.ChannelId == channelId && b.UserId == userId && b.Active);
    }
}