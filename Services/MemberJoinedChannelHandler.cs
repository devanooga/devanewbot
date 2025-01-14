namespace devanewbot.Services;

using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SlackDotNet.Options;
using SlackNet;
using SlackNet.Events;

public class MemberJoinedChannelHandler(
    IChannelBanService channelBanService,
    ISlackApiClient client,
    IOptions<SlackOptions> slackOptions,
    IWelcomeService welcomeService) : IEventHandler<MemberJoinedChannel>
{
    async Task IEventHandler<MemberJoinedChannel>.Handle(MemberJoinedChannel slackEvent)
    {
        var channelId = slackEvent.Channel;
        await welcomeService.WelcomeNewUser(channelId, slackEvent.User);

        var userId = slackEvent.User;

        var hasBan = await channelBanService.HasActiveBan(channelId, userId);
        if (hasBan)
        {
            await client.WithAccessToken(slackOptions.Value.UserToken).Conversations.Kick(channelId, userId);
        }
    }
}
