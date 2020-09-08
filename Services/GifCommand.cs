using Microsoft.Extensions.Configuration;
using SlackDotNet.Webhooks;
using Flurl;
using Flurl.Http;
using System.Threading.Tasks;
using SlackDotNet.Payloads;
using SlackDotNet;

namespace devanewbot.Services
{
    public class GifCommand : Command
    {
        public GifCommand(Slack slack, IConfiguration configuration) : base(slack, configuration)
        {
        }

        private string SearchUri { get; set; } = "https://api.cognitive.microsoft.com/bing/v7.0/images/search";

        /// <summary>
        /// Sends a gif to Slack as the user that requested the gif.
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
                Text = await Response(webhookMessage),
                IconUrl = slackUser.Profile.ImageOriginal
            });
        }

        public async Task<string> Response(WebhookMessage webhookMessage)
        {
            return await ImageSearch(webhookMessage.Text);
        }

        private async Task<string> ImageSearch(string searchTerm)
        {
            var uri = SearchUri.SetQueryParam("q", searchTerm)
                .SetQueryParam("imageType", "AnimatedGifHttps")
                .SetQueryParam("safeSearch", "Strict")
                .WithHeader("Ocp-Apim-Subscription-Key", Configuration["BingSearch:Key"]);

            var response = await uri.GetJsonAsync();

            // Grab the first gif
            return response.value[0].contentUrl;
        }
    }
}