using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SlackDotNet;
using SlackDotNet.Payloads;
using SlackDotNet.Webhooks;

namespace devanewbot.Services
{
    public class StallmanCommand : Command
    {
        private Random Random = new Random();
        private const string GALLERY = "https://stallman.org/photos/rms-working/";

        public StallmanCommand(Slack slack, IConfiguration configuration) : base(slack, configuration)
        {
        }

        /// <summary>
        /// Sends a random Stallman image to Slack
        /// </summary>
        /// <param name="webhookMessage"></param>
        /// <seealso cref="Command.HandleMessage(WebhookMessage)"/>
        /// <returns></returns>
        protected override async Task HandleMessage(WebhookMessage webhookMessage)
        {
            var slackUser = await Slack.GetUser(webhookMessage.UserId);
            await Slack.PostMessage(new ChatMessage
            {
                Channel = webhookMessage.ChannelId,
                Username = slackUser.Profile.DisplayName,
                Text = Response(webhookMessage.Text),
                IconUrl = slackUser.Profile.ImageOriginal
            });
        }

        public string Response(string text)
        {
            return "webhook worked";
        }
    }
}