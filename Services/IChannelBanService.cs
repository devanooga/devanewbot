namespace devanewbot.Services;

using System;
using System.Threading.Tasks;

public interface IChannelBanService
{
    Task AddBan(string channelId, string userId, string banningUserId, string reason, DateTime? expiresAt = null);
    Task RemoveBan(string channelId, string userId);
    Task CheckExpirations();
    Task<bool> HasActiveBan(string channelId, string userId);
}