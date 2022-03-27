namespace devanewbot.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using HtmlAgilityPack;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using SlackDotNet;
    using SlackDotNet.Payloads;
    using SlackDotNet.Webhooks;

    public class StallmanCommand : Command
    {
        private Random Random = new Random();
        private const string Gallery = "https://stallman.org/photos/rms-working/";

        public StallmanCommand(Slack slack, IConfiguration configuration, ILogger<StallmanCommand> logger) : base("stallman", slack, configuration, logger)
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
                Text = ResponseAsync(webhookMessage.Text),
                IconUrl = slackUser.Profile.ImageOriginal
            });
        }

        public string ResponseAsync(string text)
        {
            // Download the gallery page and get random link
            var galleryPage = new HtmlWeb().Load(Gallery);
            var links = galleryPage.DocumentNode
                    .SelectNodes("//p[3]/a");
            var link = Gallery + links[Random.Next(links.Count)]
                    .Attributes["href"].Value;

            // Download the resulting page and get random image link
            var imagePage = new HtmlWeb().Load(link);

            return Gallery + imagePage.DocumentNode
                    .SelectNodes("//p[2]/a/img")
                    .First()
                    .GetAttributeValue("src", "")
                    .Substring(3);
        }
    }
}
