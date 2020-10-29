using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using SlackDotNet;
using SlackDotNet.Payloads;
using SlackDotNet.Webhooks;

namespace devanewbot.Services
{
    public class StallmanCommand : Command
    {
        private Random Random = new Random();
        private const string Gallery = "https://stallman.org/photos/rms-working/";

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
                Text = await ResponseAsync(webhookMessage.Text),
                IconUrl = slackUser.Profile.ImageOriginal
            });
        }

        public async Task<string> ResponseAsync(string text)
        {
            // Download the gallery page and get random link
            var galleryPage = await Gallery.GetStringAsync();
            var link = GetRandomLink(galleryPage);

            // Download the resulting page and get random image link
            var imagePage = await link.GetStringAsync();
            var imageLink = GetRandomLink(imagePage, true);

            return imageLink;
        }

        /// <summary>
        /// Gets a random link from a page. If `image` is true, it searches for image links
        /// </summary>
        /// <param name="page"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public string GetRandomLink(string page, bool image = false)
        {
            var links = new List<string>();
            var lines = page.Split("\n");
            Regex linkRegex;
            if (image)
            {
                linkRegex = new Regex("<a href=\"(.*)\"><img src");
            }
            else
            {
                linkRegex = new Regex("<a href=\"(pages.*html)\"");
            }

            foreach (var line in lines)
            {
                var matches = linkRegex.Match(line);
                if (matches.Success)
                {
                    links.Add(matches.Groups[1].Value);
                }
            }

            if (image)
            {
                return Gallery + links[0].Substring(3);
            }

            return Gallery + links[Random.Next(links.Count)];
        }
    }
}