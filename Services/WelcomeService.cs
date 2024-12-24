namespace devanewbot.Services;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlackDotNet.Options;
using SlackNet;

public class WelcomeService(
    ISlackApiClient client,
    IOptions<SlackOptions> slackOptions,
    ILogger<WelcomeService> logger) : IWelcomeService
{

    public async Task WelcomeNewUser(string channelId, string userId)
    {
        if (channelId == slackOptions.Value.GeneralChannelId)
        {
            try {
                // Send a message that appears to be from `brb3`
                await client
                    .Chat
                    .PostMessage(new SlackNet.WebApi.Message()
                    {
                        Channel = channelId,
                        Text = $"Welcome <@{userId}>! How did you find out about us?",
                        Username = "brb3",
                        IconUrl = "https://ca.slack-edge.com/T3WU74872-U3YAGDSDD-4f181af375e3-512"
                    });
            } catch (Exception e) {
                logger.LogError(e, "Failed to welcome new user");
            }
        }
    }
}
