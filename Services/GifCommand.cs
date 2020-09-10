using Microsoft.Extensions.Configuration;
using SlackDotNet.Webhooks;
using Flurl;
using Flurl.Http;
using System.Threading.Tasks;
using SlackDotNet.Payloads;
using SlackDotNet;
using System.Collections.Generic;

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
            var url = await Response(webhookMessage);
            await Slack.PostMessage(new ChatMessage
            {
                Channel = webhookMessage.ChannelId,
                Username = slackUser.Profile.DisplayName,
                Text = "Can do! :cando:",
                IconUrl = slackUser.Profile.ImageOriginal,
                User = webhookMessage.UserId,
                Attachments = new List<ChatAttachment>
                {
                    new ChatAttachment
                    {
                        Text = webhookMessage.Text,
                        CallbackId = "gif",
                        ImageUrl = url,
                        Actions = new List<ChatAction>()
                        {
                            new ChatAction
                            {
                                Type = "button",
                                Name = "post",
                                Text = "Post",
                                Value = url,
                                Style = "primary"
                            },
                            new ChatAction
                            {
                                Type = "button",
                                Name = "cancel",
                                Text = "Nevermind",
                                Value = "cancel",
                                Style = "danger"
                            }
                        }
                    }
                }
            }, true);
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

        /// <summary>
        /// Handles interactive messages. Used for button presses on previews.
        /// </summary>
        /// <param name="interactiveMessage"></param>
        /// <returns></returns>
        public async Task HandleInteractive(InteractiveMessage interactiveMessage)
        {
            var action = interactiveMessage.Actions[0];

            if (action.Name == "post")
            {
                var slackUser = await Slack.GetUser(interactiveMessage.User.Id);
                await Slack.PostMessage(new ChatMessage
                {
                    Channel = interactiveMessage.Channel.Id,
                    Username = slackUser.Profile.DisplayName,
                    Text = action.Value,
                    IconUrl = slackUser.Profile.ImageOriginal,
                    User = interactiveMessage.User.Id,
                });
            }

            await Slack.DeleteResponse(interactiveMessage.ResponseUrl.ToString());
        }
    }
}